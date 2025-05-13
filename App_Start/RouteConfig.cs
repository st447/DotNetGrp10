using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace Health_Care_MIS
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            // Account routes should be registered first
            routes.MapRoute(
                name: "Account",
                url: "Account/{action}/{id}",
                defaults: new { controller = "Account", action = "Login", id = UrlParameter.Optional }
            );

            routes.MapRoute(
                name: "Doctor",
                url: "Doctor/{action}/{id}",
                defaults: new { controller = "Doctor", action = "Dashboard", id = UrlParameter.Optional }
            );

            routes.MapRoute(
                name: "Appointments",
                url: "Appointments/{action}/{id}",
                defaults: new { controller = "Appointments", action = "Index", id = UrlParameter.Optional }
            );

            routes.MapRoute(
                name: "Registrations",
                url: "Registrations/{action}/{id}",
                defaults: new { controller = "Registrations", action = "Index", id = UrlParameter.Optional }
            );

            routes.MapRoute(
                name: "Prescriptions",
                url: "Prescriptions/{action}/{id}",
                defaults: new { controller = "Prescriptions", action = "Index", id = UrlParameter.Optional }
            );

            routes.MapRoute(
                name: "Medications",
                url: "Medications/{action}/{id}",
                defaults: new { controller = "Medications", action = "Index", id = UrlParameter.Optional }
            );

            routes.MapRoute(
                name: "laboratoryResults",
                url: "laboratoryResults/{action}/{id}",
                defaults: new { controller = "laboratoryResults", action = "Index", id = UrlParameter.Optional }
            );

            routes.MapRoute(
                name: "Rooms",
                url: "Rooms/{action}/{id}",
                defaults: new { controller = "Rooms", action = "Index", id = UrlParameter.Optional }
            );
           
            routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}/{id}",
                defaults: new { controller = "Home", action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}
