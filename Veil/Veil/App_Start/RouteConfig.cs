/* RouteConfig.cs
 * Purpose: Routing config
 * 
 * Revision History:
 *      Drew Matheson, 2015.09.25: Created
 */ 

using System.Web.Mvc;
using System.Web.Mvc.Routing.Constraints;
using System.Web.Routing;

namespace Veil
{
    /// <summary>
    ///     Routing config for Veil
    /// </summary>
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            // Route for adding or removing an item from the wishlist
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

            // Route for a specific user's wishlist
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

            // Default route
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