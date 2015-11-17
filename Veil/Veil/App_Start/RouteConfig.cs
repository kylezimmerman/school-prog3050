using System.Web.Mvc;
using System.Web.Mvc.Routing.Constraints;
using System.Web.Routing;

namespace Veil
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            routes.MapRoute(
                name: "Wishlist",
                url: "Wishlist/{action}/{itemId}",
                defaults: new
                {
                    controller = "Wishlist",
                    action = "Index",
                    itemId = UrlParameter.Optional
                },
                constraints: new
                {
                    itemId = new GuidRouteConstraint()
                }
            );

            routes.MapRoute(
                name: "WishlistView",
                url: "Wishlist/{username}",
                defaults: new
                {
                    controller = "Wishlist",
                    action = "Index",
                    username = UrlParameter.Optional
                }
            );

            routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}/{id}",
                defaults: new
                {
                    controller = "Home",
                    action = "Index",
                    id = UrlParameter.Optional
                }
            );
        }
    }
}
