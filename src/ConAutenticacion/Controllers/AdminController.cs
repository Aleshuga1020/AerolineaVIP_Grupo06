using System;
using System.Collections.Generic;
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
            if (rol != "Administrador" && rol != "Counter" && rol != "Cliente")
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
                            c.Importe.ToString("0.00"), c.EstadoCompraCodigo, ultimo == null ? "-" : ultimo.EstadoPagoCodigo
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
                                     v.CodigoVuelo, v.FechaOperacion.ToString("dd/MM/yyyy"), v.Origen + " - " + v.Destino,
                                     v.Matricula ?? "Sin asignar", v.Capacidad.ToString(), v.Ocupados.ToString(),
                                     (v.Disponibles ?? 0).ToString(), v.EstadoVueloCodigo
                                 }).ToList();
                    break;
            }
            return View(vm);
        }

        // ---------- Gestion de usuarios: crear, editar y eliminar ----------

        [HttpGet]
        public ActionResult CrearUsuario()
        {
            ViewBag.Roles = RolesAsignables();
            return View(new CrearUsuarioVM());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CrearUsuario(CrearUsuarioVM model)
        {
            ViewBag.Roles = RolesAsignables();
            if (!ModelState.IsValid) return View(model);

            bool esSuper = User.IsInRole("SuperUsuario");
            bool rolPermitido = model.Rol == "Cliente" || model.Rol == "Counter" || (esSuper && model.Rol == "Administrador");
            if (!rolPermitido)
            {
                ModelState.AddModelError("Rol", "El rol seleccionado no esta permitido para su cuenta.");
                return View(model);
            }

            var um = GetUserManager();
            if (um.FindByName(model.UserName.Trim()) != null)
            {
                ModelState.AddModelError("UserName", "Ya existe un usuario con ese nombre.");
                return View(model);
            }

            var user = new ApplicationUser
            {
                UserName = model.UserName.Trim(),
                Email = model.Email.Trim(),
                Nombres = model.Nombres.Trim(),
                Apellidos = model.Apellidos.Trim()
            };

            var resultado = um.Create(user, model.Password);
            if (!resultado.Succeeded)
            {
                foreach (var error in resultado.Errors) ModelState.AddModelError("", error);
                return View(model);
            }

            um.AddToRole(user.Id, model.Rol);
            TempData["Mensaje"] = "Usuario " + user.UserName + " creado con el rol " + model.Rol + ".";
            return RedirectToAction("Usuarios");
        }

        [HttpGet]
        public ActionResult EditarUsuario(string id)
        {
            var user = GetUserManager().FindById(id);
            if (user == null || !PuedeGestionar(user.Id))
            {
                TempData["Error"] = "No se puede editar esta cuenta.";
                return RedirectToAction("Usuarios");
            }

            return View(new EditarUsuarioVM
            {
                Id = user.Id,
                UserName = user.UserName,
                Email = user.Email,
                Nombres = user.Nombres,
                Apellidos = user.Apellidos
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditarUsuario(EditarUsuarioVM model)
        {
            var um = GetUserManager();
            var user = um.FindById(model.Id);
            if (user == null || !PuedeGestionar(user.Id))
            {
                TempData["Error"] = "No se puede editar esta cuenta.";
                return RedirectToAction("Usuarios");
            }

            model.UserName = user.UserName;
            if (!ModelState.IsValid) return View(model);

            string email = model.Email.Trim();
            if (um.Users.Any(u => u.Email == email && u.Id != user.Id))
            {
                ModelState.AddModelError("Email", "Otro usuario ya usa ese correo electronico.");
                return View(model);
            }

            user.Email = email;
            user.Nombres = model.Nombres.Trim();
            user.Apellidos = model.Apellidos.Trim();

            var resultado = um.Update(user);
            if (!resultado.Succeeded)
            {
                foreach (var error in resultado.Errors) ModelState.AddModelError("", error);
                return View(model);
            }

            TempData["Mensaje"] = "Datos del usuario " + user.UserName + " actualizados.";
            return RedirectToAction("Usuarios");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EliminarUsuario(string id)
        {
            var um = GetUserManager();
            var user = um.FindById(id);
            if (user == null || !PuedeGestionar(id))
            {
                TempData["Error"] = "No se puede eliminar esta cuenta.";
                return RedirectToAction("Usuarios");
            }

            if (id == User.Identity.GetUserId())
            {
                TempData["Error"] = "No puede eliminar su propia cuenta.";
                return RedirectToAction("Usuarios");
            }

            if (db.Reserva.Any(r => r.UsuarioId == id))
            {
                TempData["Error"] = "No se puede eliminar a " + user.UserName + " porque tiene reservas registradas.";
                return RedirectToAction("Usuarios");
            }

            var resultado = um.Delete(user);
            TempData[resultado.Succeeded ? "Mensaje" : "Error"] = resultado.Succeeded
                ? "Usuario " + user.UserName + " eliminado."
                : "No se pudo eliminar el usuario.";
            return RedirectToAction("Usuarios");
        }

        // Un Administrador no puede modificar cuentas SuperUsuario; solo el SuperUsuario puede hacerlo.
        private bool PuedeGestionar(string usuarioId)
        {
            if (User.IsInRole("SuperUsuario")) return true;
            return !GetUserManager().IsInRole(usuarioId, "SuperUsuario");
        }

        private List<SelectListItem> RolesAsignables()
        {
            var roles = new List<SelectListItem>
            {
                new SelectListItem { Value = "Cliente", Text = "Cliente (pasajero)" },
                new SelectListItem { Value = "Counter", Text = "Counter (mostrador)" }
            };
            if (User.IsInRole("SuperUsuario"))
                roles.Add(new SelectListItem { Value = "Administrador", Text = "Administrador" });
            return roles;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}
