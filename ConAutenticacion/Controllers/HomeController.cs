using System.Web.Mvc;

namespace ConAutenticacion.Controllers
{
    public class HomeController : Controller
    {
        [AllowAnonymous]
        public ActionResult Index()
        {
            return View();
        }

        [AllowAnonymous]
        public ActionResult About()
        {
            ViewBag.Message = "Sistema de reservas de Aerolínea VIP (proyecto académico).";
            return View();
        }

        [AllowAnonymous]
        public ActionResult Contact()
        {
            ViewBag.Message = "Grupo 6 · Desarrollo Web para Integración de Tecnologías.";
            return View();
        }
    }
}
