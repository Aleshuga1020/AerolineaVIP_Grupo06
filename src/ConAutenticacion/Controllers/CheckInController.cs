using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web.Mvc;
using ConAutenticacion.Models;
using Microsoft.AspNet.Identity;

namespace ConAutenticacion.Controllers
{
    [Authorize]
    public class CheckInController : Controller
    {
        private AerolineaVIPContext db = new AerolineaVIPContext();

        // ---------- Pantalla 15: check-in ----------

        public ActionResult Index()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult BuscarReserva(string codigoReserva, string numeroDocumento)
        {
            var userId = User.Identity.GetUserId();
            string codigo = (codigoReserva ?? "").Trim().ToUpper();
            string doc = (numeroDocumento ?? "").Trim();

            if (!Regex.IsMatch(codigo, @"^RV\d{8}$") || string.IsNullOrEmpty(doc))
            {
                ModelState.AddModelError("", "Revise el codigo de reserva (RV + 8 digitos) y el numero de documento.");
                return View("Index");
            }

            var asignaciones = db.ReservaPasajeroVuelo.Include(a => a.Pasajero)
                .Where(a => a.Reserva.CodigoReserva == codigo && a.Reserva.UsuarioId == userId)
                .OrderBy(a => a.AsignacionId).ToList();

            var propias = asignaciones.Where(a => a.Pasajero.NumeroDocumento == doc).ToList();
            if (propias.Count == 0)
            {
                ModelState.AddModelError("", "No se encontro una reserva con esos datos.");
                return View("Index");
            }

            var items = propias.Select(a => ItemCheckIn(a)).ToList();
            ViewBag.Codigo = codigo;
            return View("ConfirmarCheckIn", items);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ProcesarCheckIn(int[] asignacionesSeleccionadas, bool confirmaEquipaje)
        {
            var userId = User.Identity.GetUserId();
            if (asignacionesSeleccionadas == null || asignacionesSeleccionadas.Length == 0 || !confirmaEquipaje)
            {
                TempData["Error"] = "Seleccione al menos un pasajero y confirme el equipaje.";
                return RedirectToAction("Index");
            }

            // Solo se registran asignaciones propias, pagadas (CONFIRMADA), sin check-in previo y dentro de la ventana de tiempo (48h a 1h antes).
            var asigs = db.ReservaPasajeroVuelo.Include(a => a.Pasajero).Include(a => a.Reserva)
                .Where(a => asignacionesSeleccionadas.Contains(a.AsignacionId)
                         && a.Reserva.UsuarioId == userId
                         && a.EstadoAsignacionCodigo == "CONFIRMADA").ToList();
            if (asigs.Count != asignacionesSeleccionadas.Distinct().Count())
            {
                TempData["Error"] = "Hay pasajeros no elegibles para check-in.";
                return RedirectToAction("Index");
            }

            DateTime ahora = DateTime.Now;
            foreach (var a in asigs)
            {
                var vuelo = db.vw_VueloDetalle.FirstOrDefault(v => v.VueloId == a.VueloId);
                if (vuelo == null || vuelo.EstadoVueloCodigo == "CANCELADO")
                {
                    TempData["Error"] = "El vuelo no esta disponible para check-in.";
                    return RedirectToAction("Index");
                }

                DateTime salidaProgramada = vuelo.SalidaProgramada ?? vuelo.FechaOperacion.Date.Add(vuelo.HoraSalida);
                DateTime apertura = salidaProgramada.AddHours(-48);
                DateTime cierre = salidaProgramada.AddHours(-1);

                if (ahora < apertura)
                {
                    TempData["Error"] = "El check-in se abre 48h antes de la salida programada.";
                    return RedirectToAction("Index");
                }
                if (ahora > cierre)
                {
                    TempData["Error"] = "El vuelo ya cerro/despego (el check-in cierra 1h antes de la salida).";
                    return RedirectToAction("Index");
                }
            }

            using (var tx = db.Database.BeginTransaction())
            {
                foreach (var a in asigs)
                {
                    bool yaHecho = db.CheckIn.Any(c => c.AsignacionId == a.AsignacionId && c.EstadoCheckInCodigo == "COMPLETADO");
                    if (!yaHecho)
                    {
                        db.CheckIn.Add(new CheckIn { AsignacionId = a.AsignacionId, FechaRegistro = DateTime.Now, EstadoCheckInCodigo = "COMPLETADO" });
                    }
                }
                db.SaveChanges();
                tx.Commit();
            }

            ViewBag.Codigo = asigs.First().Reserva.CodigoReserva;
            var items = asigs.Select(a => ItemCheckIn(a)).ToList();
            return View("CheckInExitoso", items);
        }

        private CheckInItemVM ItemCheckIn(ReservaPasajeroVuelo a)
        {
            var vuelo = db.vw_VueloDetalle.FirstOrDefault(v => v.VueloId == a.VueloId);
            bool confirmado = a.EstadoAsignacionCodigo == "CONFIRMADA";
            bool yaHecho = db.CheckIn.Any(c => c.AsignacionId == a.AsignacionId && c.EstadoCheckInCodigo == "COMPLETADO");

            DateTime ahora = DateTime.Now;
            bool enVentanaTiempo = false;
            string motivoTiempo = "";

            if (vuelo == null || vuelo.EstadoVueloCodigo == "CANCELADO")
            {
                motivoTiempo = "El vuelo fue cancelado o no esta disponible.";
            }
            else
            {
                DateTime salidaProgramada = vuelo.SalidaProgramada ?? vuelo.FechaOperacion.Date.Add(vuelo.HoraSalida);
                DateTime apertura = salidaProgramada.AddHours(-48);
                DateTime cierre = salidaProgramada.AddHours(-1);

                if (ahora < apertura)
                {
                    motivoTiempo = "El check-in se abre 48h antes (" + apertura.ToString("dd/MM/yyyy HH:mm") + ").";
                }
                else if (ahora > cierre)
                {
                    motivoTiempo = "El vuelo ya cerro/despego (cierre 1h antes de la salida).";
                }
                else
                {
                    enVentanaTiempo = true;
                }
            }

            string motivo = !confirmado
                ? "La compra aun no esta pagada."
                : (yaHecho
                    ? "Check-in ya registrado."
                    : (!enVentanaTiempo ? motivoTiempo : ""));

            return new CheckInItemVM
            {
                AsignacionId = a.AsignacionId,
                Pasajero = a.Pasajero.Nombres + " " + a.Pasajero.Apellidos,
                Documento = a.Pasajero.NumeroDocumento,
                Vuelo = vuelo == null ? "" : vuelo.CodigoVuelo,
                Ruta = vuelo == null ? "" : vuelo.Origen + " - " + vuelo.Destino + " (" + (vuelo.SalidaProgramada ?? vuelo.FechaOperacion.Date.Add(vuelo.HoraSalida)).ToString("dd/MM/yyyy HH:mm") + ")",
                Asiento = db.vw_EstadoAsientoVuelo
                            .Where(e => e.VueloId == a.VueloId && e.NumeroAsiento == a.NumeroAsiento)
                            .Select(e => e.CodigoAsiento).FirstOrDefault(),
                Elegible = confirmado && !yaHecho && enVentanaTiempo,
                YaRegistrado = yaHecho,
                Motivo = motivo
            };
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}
