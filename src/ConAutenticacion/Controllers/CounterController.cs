using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web.Mvc;
using ConAutenticacion.Models;
using ConAutenticacion.Services;

namespace ConAutenticacion.Controllers
{
    [Authorize(Roles = "Administrador,SuperUsuario,Counter")]
    public class CounterController : Controller
    {
        private AerolineaVIPContext db = new AerolineaVIPContext();
        private ApplicationDbContext identityDb = new ApplicationDbContext();

        // ---------- MOD-09: Atencion en Mostrador (Counter) ----------

        [HttpGet]
        public ActionResult Index()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult BuscarReserva(string codigoReserva)
        {
            string codigo = (codigoReserva ?? "").Trim().ToUpper();

            if (string.IsNullOrEmpty(codigo) || !Regex.IsMatch(codigo, @"^RV\d{8}$"))
            {
                ViewBag.Error = "Ingrese un codigo de reserva valido con formato RV seguido de 8 digitos (ej. RV00000001).";
                return View("Index");
            }

            return RedirectToAction("Detalle", new { codigo = codigo });
        }

        [HttpGet]
        public ActionResult Detalle(string codigo)
        {
            codigo = (codigo ?? "").Trim().ToUpper();

            if (!Regex.IsMatch(codigo, @"^RV\d{8}$"))
            {
                ViewBag.Error = "Ingrese un codigo de reserva valido con formato RV seguido de 8 digitos (ej. RV00000001).";
                return View("Index");
            }

            var reserva = db.Reserva.FirstOrDefault(r => r.CodigoReserva == codigo);
            if (reserva == null)
            {
                ViewBag.Error = "No se encontro ninguna reserva registrada con el codigo " + codigo + ".";
                return View("Index");
            }

            var detalle = ReservaServicio.ObtenerDetalleCompleto(db, reserva);
            var usuario = identityDb.Users.FirstOrDefault(u => u.Id == reserva.UsuarioId);

            if (usuario != null)
            {
                ViewBag.TitularNombre = (usuario.Nombres + " " + usuario.Apellidos).Trim();
                ViewBag.TitularUsuario = usuario.UserName;
                ViewBag.TitularCorreo = usuario.Email;
            }
            else
            {
                ViewBag.TitularNombre = "Usuario de demostracion (ID: " + reserva.UsuarioId + ")";
                ViewBag.TitularUsuario = reserva.UsuarioId;
                ViewBag.TitularCorreo = "No registrado en Identity";
            }

            ConfigurarCambioAsiento(detalle);
            return View("DetalleReserva", detalle);
        }

        // Cambio de asiento en mostrador: solo para reservas activas, vuelos que aun no salen
        // y pasajeros sin check-in completado. La reasignacion se valida dentro de una
        // transaccion serializable para evitar que dos mostradores ocupen el mismo asiento.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CambiarAsiento(int asignacionId, int nuevoAsiento)
        {
            var asignacion = db.ReservaPasajeroVuelo
                               .Include(a => a.Reserva)
                               .FirstOrDefault(a => a.AsignacionId == asignacionId);

            if (asignacion == null)
            {
                TempData["Error"] = "La asignacion indicada no existe.";
                return RedirectToAction("Index");
            }

            string codigo = asignacion.Reserva.CodigoReserva;

            if (asignacion.Reserva.EstadoReservaCodigo != "PENDIENTE" && asignacion.Reserva.EstadoReservaCodigo != "CONFIRMADA")
            {
                TempData["Error"] = "Solo se puede cambiar el asiento de reservas pendientes o confirmadas.";
                return RedirectToAction("Detalle", new { codigo = codigo });
            }

            if (asignacion.EstadoAsignacionCodigo != "PENDIENTE" && asignacion.EstadoAsignacionCodigo != "CONFIRMADA")
            {
                TempData["Error"] = "Este pasajero no tiene un asiento activo.";
                return RedirectToAction("Detalle", new { codigo = codigo });
            }

            if (db.CheckIn.Any(c => c.AsignacionId == asignacionId && c.EstadoCheckInCodigo == "COMPLETADO"))
            {
                TempData["Error"] = "No se puede cambiar el asiento: el pasajero ya completo el check-in.";
                return RedirectToAction("Detalle", new { codigo = codigo });
            }

            var vuelo = db.vw_VueloDetalle.FirstOrDefault(v => v.VueloId == asignacion.VueloId);
            if (vuelo == null)
            {
                TempData["Error"] = "No se encontro el vuelo de la asignacion.";
                return RedirectToAction("Detalle", new { codigo = codigo });
            }

            var salida = vuelo.SalidaProgramada ?? vuelo.FechaOperacion.Date.Add(vuelo.HoraSalida);
            if (salida <= DateTime.Now)
            {
                TempData["Error"] = "No se puede cambiar el asiento: el vuelo ya salio.";
                return RedirectToAction("Detalle", new { codigo = codigo });
            }

            if (nuevoAsiento < 1 || nuevoAsiento > 255 || nuevoAsiento == asignacion.NumeroAsiento)
            {
                TempData["Error"] = "Seleccione un asiento distinto al actual.";
                return RedirectToAction("Detalle", new { codigo = codigo });
            }

            byte numero = (byte)nuevoAsiento;

            using (var tx = db.Database.BeginTransaction(IsolationLevel.Serializable))
            {
                try
                {
                    bool libre = db.vw_EstadoAsientoVuelo.Any(s =>
                        s.VueloId == asignacion.VueloId && s.NumeroAsiento == numero && s.EstadoVisual == "DISPONIBLE");

                    if (!libre)
                    {
                        tx.Rollback();
                        TempData["Error"] = "El asiento seleccionado ya no esta disponible. Elija otro.";
                        return RedirectToAction("Detalle", new { codigo = codigo });
                    }

                    asignacion.NumeroAsiento = numero;
                    db.SaveChanges();
                    tx.Commit();
                }
                catch (Exception)
                {
                    tx.Rollback();
                    TempData["Error"] = "No se pudo cambiar el asiento. Intente nuevamente.";
                    return RedirectToAction("Detalle", new { codigo = codigo });
                }
            }

            TempData["Mensaje"] = "El asiento se cambio correctamente a " + nuevoAsiento + ".";
            return RedirectToAction("Detalle", new { codigo = codigo });
        }

        private void ConfigurarCambioAsiento(ReservaDetalleVM detalle)
        {
            var opciones = new Dictionary<int, List<SelectListItem>>();
            bool reservaActiva = detalle.Reserva.EstadoReservaCodigo == "PENDIENTE" || detalle.Reserva.EstadoReservaCodigo == "CONFIRMADA";

            foreach (var a in detalle.Asientos)
            {
                if (!reservaActiva || a.Vuelo == null) continue;
                if (a.Estado != "PENDIENTE" && a.Estado != "CONFIRMADA") continue;

                var salida = a.Vuelo.SalidaProgramada ?? a.Vuelo.FechaOperacion.Date.Add(a.Vuelo.HoraSalida);
                if (salida <= DateTime.Now) continue;

                if (db.CheckIn.Any(c => c.AsignacionId == a.AsignacionId && c.EstadoCheckInCodigo == "COMPLETADO")) continue;

                var libres = db.vw_EstadoAsientoVuelo
                    .Where(s => s.VueloId == a.Vuelo.VueloId && s.EstadoVisual == "DISPONIBLE")
                    .OrderBy(s => s.NumeroAsiento)
                    .ToList()
                    .Select(s => new SelectListItem
                    {
                        Value = ((int)s.NumeroAsiento).ToString(),
                        Text = s.CodigoAsiento
                    })
                    .ToList();

                if (libres.Count > 0) opciones[a.AsignacionId] = libres;
            }

            ViewBag.CambioAsiento = opciones;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
                identityDb.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
