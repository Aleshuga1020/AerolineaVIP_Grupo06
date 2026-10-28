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
            ViewBag.Message = "Sistema de reservas de Aerolinea VIP (proyecto academico).";
            return View();
        }

        [AllowAnonymous]
        public ActionResult Contact()
        {
            ViewBag.Message = "Grupo 6 · Desarrollo Web para Integracion de Tecnologias.";
            return View();
        }
    }
}
