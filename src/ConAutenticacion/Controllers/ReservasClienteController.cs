using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using ConAutenticacion.Models;
using ConAutenticacion.Services;
using Microsoft.AspNet.Identity;

namespace ConAutenticacion.Controllers
{
    public class ReservasClienteController : Controller
    {
        private AerolineaVIPContext db = new AerolineaVIPContext();
        private const int MaxAsientos = 7;
        private static readonly string[] EstadosActivos = { "PENDIENTE", "CONFIRMADA" };
        private static readonly string[] TiposDocumento = { "CEDULA", "PASAPORTE" };

        // ---------- 1. Buscar y listar vuelos (pantallas 5 y 6) ----------

        [AllowAnonymous]
        public ActionResult Buscar()
        {
            CargarAeropuertos();
            return View();
        }

        [AllowAnonymous]
        public ActionResult Resultados(string origen, string destino, DateTime? fecha, int? pasajeros)
        {
            var errores = new List<string>();
            if (string.IsNullOrEmpty(origen)) errores.Add("Seleccione el origen.");
            if (string.IsNullOrEmpty(destino)) errores.Add("Seleccione el destino.");
            if (!string.IsNullOrEmpty(origen) && origen == destino) errores.Add("El origen y el destino deben ser distintos.");
            if (!fecha.HasValue) errores.Add("Ingrese una fecha valida.");
            else if (fecha.Value.Date < DateTime.Today) errores.Add("La fecha no puede ser anterior a hoy.");
            if (!pasajeros.HasValue || pasajeros.Value < 1 || pasajeros.Value > MaxAsientos)
                errores.Add("Los pasajeros deben estar entre 1 y 7.");

            if (errores.Count > 0)
            {
                ViewBag.Errores = errores;
                CargarAeropuertos();
                return View("Buscar");
            }

            var dia = fecha.Value.Date;
            var vuelos = db.vw_DisponibilidadVuelo
                .Where(v => v.Origen == origen && v.Destino == destino && v.FechaOperacion == dia
                         && v.EstadoVueloCodigo != "CANCELADO" && v.Matricula != null && v.Disponibles >= pasajeros.Value)
                .OrderBy(v => v.HoraSalida)
                .ToList();

            ViewBag.Tarifas = db.vw_VueloDetalle
                .Where(v => v.Origen == origen && v.Destino == destino && v.FechaOperacion == dia)
                .ToList()
                .ToDictionary(v => v.VueloId, v => v.TarifaReferencial);
            ViewBag.Pasajeros = pasajeros.Value;
            ViewBag.Fecha = dia;
            return View(vuelos);
        }

        // ---------- 2. Mapa de asientos (pantallas 7 y 8) ----------

        [AllowAnonymous]
        public ActionResult Disponibilidad(int vueloId)
        {
            var vuelo = db.vw_VueloDetalle.FirstOrDefault(v => v.VueloId == vueloId);
            if (vuelo == null || vuelo.EstadoVueloCodigo == "CANCELADO")
            {
                TempData["Error"] = "El vuelo seleccionado no esta disponible.";
                return RedirectToAction("Buscar");
            }

            var asientos = db.vw_EstadoAsientoVuelo.Where(a => a.VueloId == vueloId)
                                                   .OrderBy(a => a.NumeroAsiento).ToList();
            ViewBag.Vuelo = vuelo;
            ViewBag.SinAeronave = !asientos.Any();
            ViewBag.Corte = DateTime.Now;
            return View(asientos);
        }

        // ---------- 3. Datos de pasajeros (pantalla 9) ----------

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Pasajeros(int vueloId, List<int> asientos)
        {
            var vm = ConstruirFlujo(vueloId, asientos ?? new List<int>(), null);
            var errores = ValidarAsientos(vm);
            if (errores.Count > 0)
            {
                TempData["Error"] = string.Join(" ", errores);
                return RedirectToAction("Disponibilidad", new { vueloId = vueloId });
            }

            // Un bloque vacio por cada asiento elegido.
            for (int i = 0; i < vm.Asientos.Count; i++)
                vm.Pasajeros.Add(new PasajeroFormulario { TipoDocumentoCodigo = "CEDULA" });

            return View(vm);
        }

        // ---------- 4. Resumen antes de confirmar (pantalla 10) ----------

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Resumen(int vueloId, List<int> asientos, List<PasajeroFormulario> pasajeros)
        {
            var vm = ConstruirFlujo(vueloId, asientos ?? new List<int>(), pasajeros);
            var errores = ValidarOperacion(vm);
            if (errores.Count > 0)
            {
                AgregarErrores(errores);
                return View("Pasajeros", vm);
            }
            return View(vm);
        }

        // ---------- 5. Registrar la reserva (pantalla 10 -> 11) ----------

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Confirmar(int vueloId, List<int> asientos, List<PasajeroFormulario> pasajeros, bool aceptaCondiciones)
        {
            var vm = ConstruirFlujo(vueloId, asientos ?? new List<int>(), pasajeros);
            var errores = ValidarOperacion(vm);
            if (!aceptaCondiciones) errores.Add("Debe aceptar las condiciones de la reserva.");
            if (errores.Count > 0)
            {
                AgregarErrores(errores);
                return View("Resumen", vm);
            }

            var userId = User.Identity.GetUserId();
            using (var tx = db.Database.BeginTransaction(IsolationLevel.Serializable))
            {
                try
                {
                    // Revalidacion definitiva dentro de la transaccion (RNP-04).
                    var ocupados = db.vw_EstadoAsientoVuelo
                        .Where(a => a.VueloId == vueloId && a.EstadoVisual != "DISPONIBLE")
                        .ToList().Select(a => (int)a.NumeroAsiento).ToList();
                    if (vm.Asientos.Any(a => ocupados.Contains(a)))
                    {
                        tx.Rollback();
                        TempData["Error"] = "Uno de los asientos seleccionados acaba de ocuparse. Seleccione otro.";
                        return RedirectToAction("Disponibilidad", new { vueloId = vueloId });
                    }

                    // Pasajeros: se reutiliza el registro si el documento ya existe (UQ_Pasajero_Documento).
                    var pasajeroIds = new List<int>();
                    foreach (var f in vm.Pasajeros)
                    {
                        var pax = db.Pasajero.FirstOrDefault(p => p.TipoDocumentoCodigo == f.TipoDocumentoCodigo
                                                               && p.NumeroDocumento == f.NumeroDocumento);
                        if (pax == null)
                        {
                            pax = new Pasajero
                            {
                                TipoDocumentoCodigo = f.TipoDocumentoCodigo,
                                NumeroDocumento = f.NumeroDocumento,
                                Nombres = f.Nombres,
                                Apellidos = f.Apellidos,
                                FechaNacimiento = f.FechaNacimiento,
                                Correo = f.Correo,
                                Telefono = f.Telefono,
                                FechaRegistro = DateTime.Now
                            };
                            db.Pasajero.Add(pax);
                            db.SaveChanges();
                        }

                        if (db.ReservaPasajeroVuelo.Any(r => r.PasajeroId == pax.PasajeroId && r.VueloId == vueloId
                                                          && EstadosActivos.Contains(r.EstadoAsignacionCodigo)))
                        {
                            tx.Rollback();
                            TempData["Error"] = "Un pasajero ya tiene un asiento activo en este vuelo.";
                            return RedirectToAction("Disponibilidad", new { vueloId = vueloId });
                        }
                        pasajeroIds.Add(pax.PasajeroId);
                    }

                    var reserva = new Reserva { UsuarioId = userId, FechaCreacion = DateTime.Now, EstadoReservaCodigo = "PENDIENTE" };
                    db.Reserva.Add(reserva);
                    db.SaveChanges();
                    db.Entry(reserva).Reload(); // CodigoReserva es columna calculada en la BD

                    for (int i = 0; i < vm.Asientos.Count; i++)
                    {
                        db.ReservaPasajeroVuelo.Add(new ReservaPasajeroVuelo
                        {
                            ReservaId = reserva.ReservaId,
                            PasajeroId = pasajeroIds[i],
                            VueloId = vueloId,
                            NumeroAsiento = (byte)vm.Asientos[i],
                            ImporteTramo = vm.Tarifa,
                            EstadoAsignacionCodigo = "PENDIENTE",
                            FechaAsignacion = DateTime.Now
                        });
                    }
                    db.SaveChanges();
                    tx.Commit();

                    TempData["Mensaje"] = "Reserva " + reserva.CodigoReserva + " registrada. Estado: pendiente de pago.";
                    return RedirectToAction("Pagar", "Compras", new { id = reserva.ReservaId });
                }
                catch (Exception)
                {
                    tx.Rollback();
                    TempData["Error"] = "No se pudo registrar la reserva. Intente nuevamente.";
                    return RedirectToAction("Disponibilidad", new { vueloId = vueloId });
                }
            }
        }

        // ---------- 6. Mis reservas y cancelacion (pantallas 13 y 14) ----------

        [Authorize]
        public ActionResult MisReservas()
        {
            var userId = User.Identity.GetUserId();
            var reservas = db.Reserva.Where(r => r.UsuarioId == userId)
                                     .OrderByDescending(r => r.ReservaId).ToList();
            var detalles = reservas.Select(r => ReservaServicio.Detalle(db, r)).ToList();
            return View(detalles);
        }

        [Authorize]
        public ActionResult Gestionar(int id)
        {
            var reserva = ReservaDeUsuario(id);
            if (reserva == null) return HttpNotFound();
            return View(ReservaServicio.Detalle(db, reserva));
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Cancelar(int id)
        {
            var reserva = ReservaDeUsuario(id);
            if (reserva == null) return HttpNotFound();

            if (reserva.EstadoReservaCodigo != "PENDIENTE")
            {
                TempData["Error"] = "Solo se pueden cancelar reservas pendientes de pago. El reembolso esta pendiente de aprobacion (DP-08).";
                return RedirectToAction("Gestionar", new { id = id });
            }

            using (var tx = db.Database.BeginTransaction())
            {
                reserva.EstadoReservaCodigo = "CANCELADA";
                foreach (var a in db.ReservaPasajeroVuelo.Where(x => x.ReservaId == id))
                    a.EstadoAsignacionCodigo = "CANCELADA";
                foreach (var c in db.Compra.Where(x => x.ReservaId == id && x.EstadoCompraCodigo == "PENDIENTE"))
                    c.EstadoCompraCodigo = "ANULADA";
                db.SaveChanges();
                tx.Commit();
            }

            TempData["Mensaje"] = "Reserva " + reserva.CodigoReserva + " cancelada. Los asientos quedan disponibles.";
            return RedirectToAction("MisReservas");
        }

        // ---------- Auxiliares ----------

        private void CargarAeropuertos()
        {
            ViewBag.Aeropuertos = new SelectList(db.Aeropuerto.Where(a => a.Activo).OrderBy(a => a.Localidad).ToList(), "CodigoIATA", "Localidad");
        }

        private Reserva ReservaDeUsuario(int id)
        {
            var userId = User.Identity.GetUserId();
            return db.Reserva.FirstOrDefault(r => r.ReservaId == id && r.UsuarioId == userId);
        }

        private FlujoReservaVM ConstruirFlujo(int vueloId, List<int> asientos, List<PasajeroFormulario> pasajeros)
        {
            var vuelo = db.vw_VueloDetalle.FirstOrDefault(v => v.VueloId == vueloId);
            var vm = new FlujoReservaVM
            {
                VueloId = vueloId,
                Vuelo = vuelo,
                Asientos = asientos,
                Tarifa = vuelo == null ? 0 : vuelo.TarifaReferencial
            };
            vm.CodigosAsiento = db.vw_EstadoAsientoVuelo.Where(a => a.VueloId == vueloId).ToList()
                                   .ToDictionary(a => (int)a.NumeroAsiento, a => a.CodigoAsiento);
            if (pasajeros != null)
            {
                foreach (var p in pasajeros)
                {
                    if (p == null) continue;
                    p.NumeroDocumento = (p.NumeroDocumento ?? "").Trim();
                    p.Nombres = (p.Nombres ?? "").Trim();
                    p.Apellidos = (p.Apellidos ?? "").Trim();
                    p.Correo = string.IsNullOrWhiteSpace(p.Correo) ? null : p.Correo.Trim();
                    p.Telefono = string.IsNullOrWhiteSpace(p.Telefono) ? null : p.Telefono.Trim();
                }
                vm.Pasajeros = pasajeros;
            }
            return vm;
        }

        private List<string> ValidarOperacion(FlujoReservaVM vm)
        {
            var errores = ValidarAsientos(vm);
            if (errores.Count == 0) errores.AddRange(ValidarPasajeros(vm.Pasajeros, vm.Asientos.Count));
            return errores;
        }

        private List<string> ValidarAsientos(FlujoReservaVM vm)
        {
            var e = new List<string>();
            if (vm.Vuelo == null || vm.Vuelo.EstadoVueloCodigo == "CANCELADO")
            {
                e.Add("El vuelo seleccionado no esta disponible.");
                return e;
            }
            if (vm.Asientos.Count < 1 || vm.Asientos.Count > MaxAsientos)
            {
                e.Add("Seleccione entre 1 y 7 asientos.");
                return e;
            }
            if (vm.Asientos.Distinct().Count() != vm.Asientos.Count)
                e.Add("No puede seleccionar el mismo asiento dos veces.");

            var libres = db.vw_EstadoAsientoVuelo
                .Where(a => a.VueloId == vm.VueloId && a.EstadoVisual == "DISPONIBLE")
                .ToList().Select(a => (int)a.NumeroAsiento).ToList();
            foreach (var n in vm.Asientos.Where(n => !libres.Contains(n)).Distinct())
            {
                string codigo = vm.CodigosAsiento.ContainsKey(n) ? vm.CodigosAsiento[n] : n.ToString();
                e.Add("El asiento " + codigo + " ya no esta disponible.");
            }

            int activos = db.ReservaPasajeroVuelo.Count(r => r.VueloId == vm.VueloId && EstadosActivos.Contains(r.EstadoAsignacionCodigo));
            if (activos + vm.Asientos.Count > MaxAsientos)
                e.Add("El vuelo no tiene capacidad para esa cantidad de pasajeros.");
            return e;
        }

        private List<string> ValidarPasajeros(List<PasajeroFormulario> ps, int cantidad)
        {
            var e = new List<string>();
            if (ps == null || ps.Count != cantidad)
            {
                e.Add("Complete los datos de todos los pasajeros.");
                return e;
            }

            var documentos = new HashSet<string>();
            for (int i = 0; i < ps.Count; i++)
            {
                var p = ps[i];
                string n = "Pasajero " + (i + 1) + ": ";
                if (p == null || !TiposDocumento.Contains(p.TipoDocumentoCodigo))
                {
                    e.Add(n + "seleccione el tipo de documento.");
                    continue;
                }

                if (!Validaciones.DocumentoValido(p.TipoDocumentoCodigo, p.NumeroDocumento))
                    e.Add(n + (p.TipoDocumentoCodigo == "CEDULA"
                        ? "la cedula debe tener 10 digitos."
                        : "el pasaporte debe tener entre 5 y 20 caracteres alfanumericos."));
                else if (!documentos.Add(p.TipoDocumentoCodigo + p.NumeroDocumento))
                    e.Add(n + "el documento esta repetido en la reserva.");

                if (!Validaciones.NombreValido(p.Nombres)) e.Add(n + "nombres invalidos (solo letras y espacios).");
                if (!Validaciones.NombreValido(p.Apellidos)) e.Add(n + "apellidos invalidos (solo letras y espacios).");
                if (p.FechaNacimiento.HasValue && (p.FechaNacimiento.Value < new DateTime(1900, 1, 1) || p.FechaNacimiento.Value > DateTime.Today))
                    e.Add(n + "fecha de nacimiento invalida.");
                if (p.Correo != null && !Validaciones.CorreoValido(p.Correo)) e.Add(n + "correo invalido.");
                if (p.Telefono != null && !Validaciones.TelefonoValido(p.Telefono)) e.Add(n + "telefono invalido.");
            }
            return e;
        }

        private void AgregarErrores(List<string> errores)
        {
            foreach (var e in errores) ModelState.AddModelError("", e);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}
