using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using ConAutenticacion.Models;
using Microsoft.AspNet.Identity;

namespace ConAutenticacion.Controllers
{
    [Authorize]
    public class ComprobantesController : Controller
    {
        private AerolineaVIPContext db = new AerolineaVIPContext();

        // ---------- Pantalla 16: comprobantes académicos propios ----------

        public ActionResult Index()
        {
            var userId = User.Identity.GetUserId();
            var comprobantes = db.Comprobante
                .Include(c => c.Compra.Reserva)
                .Where(c => c.Compra.Reserva.UsuarioId == userId)
                .OrderByDescending(c => c.ComprobanteId)
                .ToList();
            return View(comprobantes);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}
