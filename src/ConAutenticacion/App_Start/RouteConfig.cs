using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace ConAutenticacion
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            routes.MapRoute(
                name: "Pantalla5_Buscar",
                url: "Vuelos/Buscar",
                defaults: new { controller = "ReservasCliente", action = "Buscar" }
            );

            routes.MapRoute(
                name: "Pantalla6_Resultados",
                url: "Vuelos/Resultados",
                defaults: new { controller = "ReservasCliente", action = "Resultados" }
            );

            routes.MapRoute(
                name: "Pantalla7_Disponibilidad",
                url: "Vuelos/Disponibilidad/{vueloId}",
                defaults: new { controller = "ReservasCliente", action = "Disponibilidad" }
            );

            routes.MapRoute(
                name: "Pantalla8_SeleccionAsiento",
                url: "Reservas/SeleccionAsiento/{vueloId}",
                defaults: new { controller = "ReservasCliente", action = "SeleccionAsiento" }
            );

            routes.MapRoute(
                name: "Pantalla9_Pasajeros",
                url: "Reservas/Pasajeros",
                defaults: new { controller = "ReservasCliente", action = "Pasajeros" }
            );

            routes.MapRoute(
                name: "Pantalla10_Confirmar",
                url: "Reservas/Confirmar",
                defaults: new { controller = "ReservasCliente", action = "Confirmar" }
            );

            routes.MapRoute(
                name: "Pantalla13_MisReservas",
                url: "Reservas/MisReservas",
                defaults: new { controller = "ReservasCliente", action = "MisReservas" }
            );

            routes.MapRoute(
                name: "Pantalla14_Gestionar",
                url: "Reservas/Gestionar/{reservaId}",
                defaults: new { controller = "ReservasCliente", action = "Gestionar" }
            );

            routes.MapRoute(
                name: "Pantalla11_Pagar",
                url: "Compras/Pagar/{reservaId}",
                defaults: new { controller = "Compras", action = "Pagar" }
            );

            routes.MapRoute(
                name: "Pantalla12_Confirmacion",
                url: "Compras/Confirmacion/{reservaId}",
                defaults: new { controller = "Compras", action = "Confirmacion" }
            );

            routes.MapRoute(
                name: "Pantalla18_AdminVuelos",
                url: "Admin/Vuelos",
                defaults: new { controller = "Vuelos", action = "Index" }
            );

            routes.MapRoute(
                name: "Pantalla19_AdminAeronaves",
                url: "Admin/Aeronaves",
                defaults: new { controller = "Aeronaves", action = "Index" }
            );

            routes.MapRoute(
                name: "Pantalla20_AdminGestion",
                url: "Admin/Gestion",
                defaults: new { controller = "Admin", action = "Usuarios" }
            );

            routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}/{id}",
                defaults: new { controller = "Home", action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}
