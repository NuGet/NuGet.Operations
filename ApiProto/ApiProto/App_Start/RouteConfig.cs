using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace ApiProto
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.MapRoute(
                "Home",
                "",
                new { controller = "Home", action = "Index" });

            routes.MapRoute(
                "Search",
                url: "api/v3/search",
                defaults: new { controller = "Api", action = "Search" },
                constraints: new { httpMethod = new HttpMethodConstraint("GET") });

            routes.MapRoute(
                "Package",
                "api/v3/package/{id}/{version}",
                defaults: new { controller = "Api", action = "Package" },
                constraints: new { httpMethod = new HttpMethodConstraint("GET") });

            routes.MapRoute(
                "PackageRegistration",
                "api/v3/package/{id}",
                defaults: new { controller = "Api", action = "PackageRegistration" },
                constraints: new { httpMethod = new HttpMethodConstraint("GET") });

            routes.MapRoute(
                "Owner",
                "api/v3/owner/{username}",
                defaults: new { controller = "Api", action = "Owner" },
                constraints: new { httpMethod = new HttpMethodConstraint("GET") });

            routes.MapRoute(
                "PackageList",
                "api/v3/packagelist/{page}",
                defaults: new { controller = "Api", action = "PackageList" },
                constraints: new { httpMethod = new HttpMethodConstraint("GET") });

            routes.MapRoute(
                "PackageByDate",
                "api/v3/packagebydate/{page}",
                defaults: new { controller = "Api", action = "PackageByDate" },
                constraints: new { httpMethod = new HttpMethodConstraint("GET") });

            routes.MapRoute(
                "PackageContent",
                "api/v3/packagecontent/{id}/{version}",
                defaults: new { controller = "Api", action = "PackageContent" },
                constraints: new { httpMethod = new HttpMethodConstraint("GET") });
        }
    }
}