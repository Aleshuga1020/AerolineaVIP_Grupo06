using System.Data.Entity;
using System.Linq;
using ConAutenticacion.Models;

namespace ConAutenticacion.Services
{
    // Arma el detalle de una reserva (pasajeros, asientos, vuelo, compra y último pago).
    public static class ReservaServicio
    {
        public static ReservaDetalleVM Detalle(AerolineaVIPContext db, Reserva reserva)
        {
            var vm = new ReservaDetalleVM { Reserva = reserva };

            vm.Compra = db.Compra.Where(c => c.ReservaId == reserva.ReservaId)
                                 .OrderByDescending(c => c.CompraId).FirstOrDefault();
            if (vm.Compra != null)
            {
                vm.UltimoPago = db.Pago.Where(p => p.CompraId == vm.Compra.CompraId)
                                       .OrderByDescending(p => p.PagoId).FirstOrDefault();
            }

            var asignaciones = db.ReservaPasajeroVuelo.Include(a => a.Pasajero)
                                 .Where(a => a.ReservaId == reserva.ReservaId)
                                 .OrderBy(a => a.AsignacionId).ToList();

            decimal suma = 0;
            foreach (var a in asignaciones)
            {
                vm.Asientos.Add(new AsientoVM
                {
                    AsignacionId = a.AsignacionId,
                    Pasajero = a.Pasajero,
                    CodigoAsiento = CodigoAsiento(db, a.VueloId, a.NumeroAsiento),
                    Vuelo = db.vw_VueloDetalle.FirstOrDefault(v => v.VueloId == a.VueloId),
                    Importe = a.ImporteTramo,
                    Estado = a.EstadoAsignacionCodigo
                });
                suma += a.ImporteTramo;
            }

            vm.Total = vm.Compra != null ? vm.Compra.Importe : suma;
            return vm;
        }

        public static string CodigoAsiento(AerolineaVIPContext db, int vueloId, byte numero)
        {
            return db.vw_EstadoAsientoVuelo
                     .Where(e => e.VueloId == vueloId && e.NumeroAsiento == numero)
                     .Select(e => e.CodigoAsiento)
                     .FirstOrDefault();
        }
    }
}
