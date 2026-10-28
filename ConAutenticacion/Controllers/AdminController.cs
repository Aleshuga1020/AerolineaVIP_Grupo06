using System;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using ConAutenticacion.Models;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin;

namespace ConAutenticacion.Controllers
{
    [Authorize(Roles = "Administrador,SuperUsuario")]
    public class AdminController : Controller
    {
        private AerolineaVIPContext db = new AerolineaVIPContext();

        private ApplicationUserManager GetUserManager()
        {
            return HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>();
        }

        // ---------- Pantalla 17: panel de accesos (sin dashboard) ----------

        public ActionResult Index()
        {
            return View();
        }

        // ---------- Pantalla 20: usuarios y pasajeros ----------

        public ActionResult Usuarios()
        {
            var um = GetUserManager();
            var usuarios = um.Users.OrderBy(u => u.UserName).ToList()
                .Select(u => new GestionUsuarioVM
                {
                    Id = u.Id,
                    UserName = u.UserName,
                    Email = u.Email,
                    Nombres = u.Nombres,
                    Apellidos = u.Apellidos,
                    Roles = string.Join(", ", um.GetRoles(u.Id))
                }).ToList();

            ViewBag.Pasajeros = db.Pasajero.OrderBy(p => p.Apellidos).Take(200).ToList();
            return View(usuarios);
        }

        [Authorize(Roles = "SuperUsuario")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CambiarRol(string userId, string rol, bool asignar)
        {
            // Solo se gestionan roles no privilegiados desde la interfaz; SuperUsuario se crea por codigo.
            if (rol != "Administrador" && rol != "Cliente")
            {
                TempData["Error"] = "Rol no permitido.";
                return RedirectToAction("Usuarios");
            }

            var um = GetUserManager();
            if (asignar) um.AddToRole(userId, rol);
            else um.RemoveFromRole(userId, rol);

            TempData["Mensaje"] = "Roles actualizados.";
            return RedirectToAction("Usuarios");
        }

        // ---------- Pantalla 21: consultas (sin exportacion ni graficos) ----------

        public ActionResult Reportes(string consulta, DateTime? desde, DateTime? hasta)
        {
            consulta = string.IsNullOrEmpty(consulta) ? "ocupacion" : consulta;
            var d = desde ?? DateTime.Today;
            var h = hasta ?? DateTime.Today.AddDays(30);
            var vm = new ReporteVM { Consulta = consulta, Desde = d, Hasta = h, Corte = DateTime.Now };

            if (h < d || (h - d).TotalDays > 366)
            {
                vm.Mensaje = "Rango de fechas invalido: la fecha final debe ser posterior a la inicial y el rango no puede superar 366 dias.";
                return View(vm);
            }

            var hHasta = h.AddDays(1); // limite exclusivo
            switch (consulta)
            {
                case "reservas":
                    vm.Encabezados = new[] { "Estado de la reserva", "Cantidad" };
                    vm.Filas = db.Reserva.Where(r => r.FechaCreacion >= d && r.FechaCreacion < hHasta)
                                 .ToList()
                                 .GroupBy(r => r.EstadoReservaCodigo)
                                 .Select(g => new[] { g.Key, g.Count().ToString() }).ToList();
                    break;

                case "pagos":
                    vm.Encabezados = new[] { "Compra", "Reserva", "Fecha", "Importe (USD)", "Estado compra", "Ultimo pago" };
                    var compras = db.Compra.Include(c => c.Reserva)
                                    .Where(c => c.FechaCompra >= d && c.FechaCompra < hHasta)
                                    .OrderBy(c => c.CompraId).ToList();
                    vm.Filas = compras.Select(c =>
                    {
                        var ultimo = db.Pago.Where(p => p.CompraId == c.CompraId).OrderByDescending(p => p.PagoId).FirstOrDefault();
                        return new[]
                        {
                            c.CompraId.ToString(), c.Reserva.CodigoReserva, c.FechaCompra.ToString("dd/MM/yyyy"),
                            c.Importe.ToString("0.00"), c.EstadoCompraCodigo, ultimo == null ? "—" : ultimo.EstadoPagoCodigo
                        };
                    }).ToList();
                    break;

                default:
                    vm.Consulta = "ocupacion";
                    vm.Encabezados = new[] { "Vuelo", "Fecha", "Ruta", "Aeronave", "Capacidad", "Ocupados", "Disponibles", "Estado" };
                    vm.Filas = db.vw_DisponibilidadVuelo
                                 .Where(v => v.FechaOperacion >= d && v.FechaOperacion <= h)
                                 .OrderBy(v => v.FechaOperacion).ThenBy(v => v.HoraSalida)
                                 .ToList()
                                 .Select(v => new[]
                                 {
                                     v.CodigoVuelo, v.FechaOperacion.ToString("dd/MM/yyyy"), v.Origen + " → " + v.Destino,
                                     v.Matricula ?? "Sin asignar", v.Capacidad.ToString(), v.Ocupados.ToString(),
                                     (v.Disponibles ?? 0).ToString(), v.EstadoVueloCodigo
                                 }).ToList();
                    break;
            }
            return View(vm);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}
