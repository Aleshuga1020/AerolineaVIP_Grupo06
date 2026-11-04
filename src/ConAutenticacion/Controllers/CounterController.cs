using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web.Mvc;
using ConAutenticacion.Models;
using ConAutenticacion.Services;

namespace ConAutenticacion.Controllers
{
    [Authorize(Roles = "Administrador,SuperUsuario")]
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

            return View("DetalleReserva", detalle);
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

