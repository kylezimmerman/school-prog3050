/* UnityConfig.cs
 * Purpose: Configuration class for use of UnityContainer in Veil
 * 
 * Revision History:
 *      Drew Matheson, 2015.09.29: Created
 */ 

using System;
using System.Web;
using Microsoft.AspNet.Identity;
using Microsoft.Owin.Security;
using Microsoft.Practices.Unity;
using Veil.DataAccess;
using Veil.DataAccess.Interfaces;
using Veil.Helpers;
using Veil.Services;
using Veil.Services.Interfaces;

namespace Veil
{
    /// <summary>
    ///     Static class containing configuration for Unity Container
    /// </summary>
    public static class UnityConfig
    {
        private static Lazy<IUnityContainer> container = new Lazy<IUnityContainer>(
            () =>
            {
                var container = new UnityContainer();
                RegisterComponents(container);
                return container;
            });

        /// <summary>
        ///     Returns the configured Unity container.
        ///     Can be used to further configure the container
        /// </summary>
        /// <returns>
        ///     The configured <see cref="IUnityContainer"/>
        /// </returns>
        public static IUnityContainer GetConfiguredContainer()
        {
            return container.Value;
        }

        /// <summary>
        ///     Registers the injectable components for Veil
        /// </summary>
        /// <param name="container">
        ///     The <see cref="IUnityContainer"/> to register types on
        /// </param>
        private static void RegisterComponents(IUnityContainer container)
        {
            // register all your components with the container here
            // it is NOT necessary to register your controllers

            // Used by controllers and anywhere except UserStore contruction
            container.RegisterType<IVeilDataAccess, VeilDataContext>(new HierarchicalLifetimeManager());

            container.RegisterType<IGuidUserIdGetter, GuidUserIdGetter>(
                new ContainerControlledLifetimeManager());

            container.RegisterType<VeilUserManager>(new HierarchicalLifetimeManager());
            container.RegisterType<VeilSignInManager>(new HierarchicalLifetimeManager());
            container.RegisterType<IStripeService, StripeService>();
            container.RegisterType<IShippingCostService, ShippingCostService>();

            // Used by VeilUserManager
            container.RegisterType<IIdentityMessageService, EmailService>(
                new HierarchicalLifetimeManager());

            // Note: IDataProtectionProvider, required by VeilUserManager, is setup in Startup.Auth.cs

            // This is required for VeilSignInManager's constructor
            container.RegisterType<IAuthenticationManager>(
                new InjectionFactory(c => HttpContext.Current.GetOwinContext().Authentication));
        }
    }
}