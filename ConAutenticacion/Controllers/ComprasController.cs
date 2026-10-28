using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using ConAutenticacion.Models;
using ConAutenticacion.Services;
using Microsoft.AspNet.Identity;

namespace ConAutenticacion.Controllers
{
    [Authorize]
    public class ComprasController : Controller
    {
        private AerolineaVIPContext db = new AerolineaVIPContext();

        // ---------- Pantalla 11: pago simulado ----------

        public ActionResult Pagar(int id)
        {
            var userId = User.Identity.GetUserId();
            var reserva = db.Reserva.FirstOrDefault(r => r.ReservaId == id && r.UsuarioId == userId);
            if (reserva == null) return HttpNotFound();

            if (reserva.EstadoReservaCodigo == "CONFIRMADA")
                return RedirectToAction("Confirmacion", new { id = id });
            if (reserva.EstadoReservaCodigo != "PENDIENTE")
            {
                TempData["Error"] = "Esta reserva no se puede pagar.";
                return RedirectToAction("MisReservas", "ReservasCliente");
            }

            var compra = db.Compra.FirstOrDefault(c => c.ReservaId == id && c.EstadoCompraCodigo == "PENDIENTE");
            if (compra == null)
            {
                decimal total = db.ReservaPasajeroVuelo.Where(a => a.ReservaId == id)
                                  .Sum(a => (decimal?)a.ImporteTramo) ?? 0m;
                compra = new Compra
                {
                    ReservaId = id,
                    FechaCompra = DateTime.Now,
                    Importe = total,
                    MonedaCodigo = "USD",
                    EstadoCompraCodigo = "PENDIENTE",
                    ClaveIdempotencia = Guid.NewGuid()
                };
                db.Compra.Add(compra);
                db.SaveChanges();
            }

            ViewBag.Detalle = ReservaServicio.Detalle(db, reserva);
            ViewBag.Metodos = db.MetodoPago.Where(m => m.Activo).ToList();
            ViewBag.Marcas = db.MarcaTarjeta.Where(m => m.Activa).ToList();
            return View(compra);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ProcesarPago(int compraId, string metodoPagoCodigo, string marcaTarjetaCodigo,
                                         string numeroTarjeta, string titular, string vencimiento, string codigoSeguridad)
        {
            var userId = User.Identity.GetUserId();
            var compra = db.Compra.Find(compraId);
            if (compra == null) return HttpNotFound();

            var reserva = db.Reserva.FirstOrDefault(r => r.ReservaId == compra.ReservaId && r.UsuarioId == userId);
            if (reserva == null) return HttpNotFound();

            if (compra.EstadoCompraCodigo != "PENDIENTE")
            {
                TempData["Error"] = "La compra ya fue procesada.";
                return RedirectToAction("Confirmacion", new { id = reserva.ReservaId });
            }

            numeroTarjeta = (numeroTarjeta ?? "").Replace(" ", "").Trim();
            codigoSeguridad = (codigoSeguridad ?? "").Trim();
            var metodo = db.MetodoPago.Find(metodoPagoCodigo);
            var marca = db.MarcaTarjeta.Find(marcaTarjetaCodigo);

            var errores = new List<string>();
            if (metodo == null || !metodo.Activo) errores.Add("Seleccione un método de pago.");
            if (marca == null || !marca.Activa) errores.Add("Seleccione la marca de la tarjeta.");
            if (marca != null)
            {
                if (!Validaciones.SoloDigitos(numeroTarjeta) || numeroTarjeta.Length != marca.LongitudNumero)
                    errores.Add("El número de tarjeta debe tener " + marca.LongitudNumero + " dígitos.");
                else if (!Validaciones.EsLuhnValido(numeroTarjeta))
                    errores.Add("El número de tarjeta no es válido.");
                if (!Validaciones.SoloDigitos(codigoSeguridad) || codigoSeguridad.Length != marca.LongitudCodigoSeguridad)
                    errores.Add("El código de seguridad debe tener " + marca.LongitudCodigoSeguridad + " dígitos.");
            }
            if (!Validaciones.NombreValido(titular)) errores.Add("Ingrese el nombre del titular.");
            if (!Validaciones.VencimientoValido(vencimiento)) errores.Add("El vencimiento debe tener formato MM/AA y no estar vencido.");

            if (errores.Count > 0)
            {
                TempData["Error"] = string.Join(" ", errores);
                return RedirectToAction("Pagar", new { id = reserva.ReservaId });
            }

            // Regla académica de simulación (documentada): tarjetas que terminan en 0000 se rechazan.
            bool rechazado = numeroTarjeta.EndsWith("0000");
            int pagosPrevios = db.Pago.Count(p => p.CompraId == compra.CompraId);

            var pago = new Pago
            {
                CompraId = compra.CompraId,
                FechaPago = DateTime.Now,
                Importe = compra.Importe,
                EstadoPagoCodigo = rechazado ? "RECHAZADO" : "APROBADO",
                MetodoPagoCodigo = metodo.Codigo,
                MarcaTarjetaCodigo = marca.Codigo,
                UltimosCuatroDigitos = numeroTarjeta.Substring(numeroTarjeta.Length - 4),
                NombreTitular = titular.Trim(),
                CodigoOperacion = "SIM-" + reserva.CodigoReserva + "-" + (pagosPrevios + 1).ToString("D2"),
                EsSimulado = true,
                MensajeResultado = rechazado ? "Pago simulado rechazado (regla académica)." : "Pago simulado aprobado (académico)."
            };

            using (var tx = db.Database.BeginTransaction())
            {
                try
                {
                    db.Pago.Add(pago);
                    if (!rechazado)
                    {
                        compra.EstadoCompraCodigo = "PAGADA";
                        reserva.EstadoReservaCodigo = "CONFIRMADA";
                        foreach (var a in db.ReservaPasajeroVuelo.Where(x => x.ReservaId == reserva.ReservaId))
                            a.EstadoAsignacionCodigo = "CONFIRMADA";

                        db.Comprobante.Add(new Comprobante
                        {
                            CompraId = compra.CompraId,
                            TipoComprobanteCodigo = "CONFIRMACION",
                            NumeroComprobante = "CNF-" + compra.CompraId.ToString("D6"),
                            FechaEmision = DateTime.Now,
                            EstadoComprobanteCodigo = "EMITIDO"
                        });
                        db.Comprobante.Add(new Comprobante
                        {
                            CompraId = compra.CompraId,
                            TipoComprobanteCodigo = "FACTURA",
                            NumeroComprobante = "ACAD-" + compra.CompraId.ToString("D6"),
                            FechaEmision = DateTime.Now,
                            EstadoComprobanteCodigo = "EMITIDO"
                        });
                    }
                    db.SaveChanges();
                    tx.Commit();
                }
                catch (Exception)
                {
                    tx.Rollback();
                    TempData["Error"] = "Error al procesar el pago simulado. Intente nuevamente.";
                    return RedirectToAction("Pagar", new { id = reserva.ReservaId });
                }
            }

            if (rechazado)
            {
                TempData["Error"] = "Pago simulado rechazado. La reserva sigue pendiente; puede intentar con otra tarjeta.";
                return RedirectToAction("Pagar", new { id = reserva.ReservaId });
            }
            TempData["Mensaje"] = "Compra " + reserva.CodigoReserva + " pagada. Pago simulado aprobado " + pago.CodigoOperacion + ".";
            return RedirectToAction("Confirmacion", new { id = reserva.ReservaId });
        }

        // ---------- Pantalla 12: confirmación ----------

        public ActionResult Confirmacion(int id)
        {
            var userId = User.Identity.GetUserId();
            var reserva = db.Reserva.FirstOrDefault(r => r.ReservaId == id && r.UsuarioId == userId);
            if (reserva == null) return HttpNotFound();
            return View(ReservaServicio.Detalle(db, reserva));
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}
