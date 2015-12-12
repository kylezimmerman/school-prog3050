/* Global.asax.cs
 * Purpose: Setup the app and respond to application level events
 * 
 * Revision History:
 *      Drew Matheson, 2015.09.25: Created
 */

using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using Microsoft.Practices.Unity;
using Unity.Mvc5;

namespace Veil
{
    /// <summary>
    ///     Class for responding to application level events
    /// </summary>
    public class MvcApplication : System.Web.HttpApplication
    {
        /// <summary>
        ///     Application Start event which setups the application
        /// </summary>
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();

            // Configure the app to use Unity for dependency injection
            IUnityContainer container = UnityConfig.GetConfiguredContainer();

            DependencyResolver.SetResolver(new UnityDependencyResolver(container));

            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
        }
    }
}