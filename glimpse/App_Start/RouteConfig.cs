﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace Glimpse
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("errorlog");

            routes.IgnoreRoute("errorlog/{*anything}");

            routes.IgnoreRoute("Content/img");

            routes.MapRoute(
                name: "AboutUs",
                url: "AboutUs/{action}",
                defaults: new { controller = "AboutUs", action = "Index" }
            );

            routes.MapRoute(
                name: "MainScreen",
                url: "{action}",
                defaults: new { controller = "Home", action = "Index" }
            );

            routes.MapRoute(
                name: "AsyncMails",
                url: "async/{action}/{id}",
                defaults: new { controller = "AsyncMails", id = UrlParameter.Optional }
            );

            routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}",
                defaults: new { controller = "Home", action = "Index" }
            );
        }
    }
}