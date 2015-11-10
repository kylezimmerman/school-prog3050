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
    public static class UnityConfig
    {
        private static Lazy<IUnityContainer> container = new Lazy<IUnityContainer>(
            () =>
            {
                var container = new UnityContainer();
                RegisterComponents(container);
                return container;
            }); 

        public static IUnityContainer GetConfiguredContainer()
        {
            return container.Value;
        }

        private static void RegisterComponents(IUnityContainer container)
        {
            // register all your components with the container here
            // it is NOT necessary to register your controllers
            // e.g. container.RegisterType<ITestService, TestService>();

            // Used by controllers and anywhere except UserStore contruction
            container.RegisterType<IVeilDataAccess, VeilDataContext>(new HierarchicalLifetimeManager());

            container.RegisterType<IGuidUserIdGetter, GuidUserIdGetter>(
                new ContainerControlledLifetimeManager());

            container.RegisterType<VeilUserManager>(new HierarchicalLifetimeManager());
            container.RegisterType<VeilSignInManager>(new HierarchicalLifetimeManager());
            container.RegisterType<IStripeService, StripeService>();

            // Used by VeilUserManager
            container.RegisterType<IIdentityMessageService, EmailService>(new HierarchicalLifetimeManager());

            // Note: IDataProtectionProvider required by VeilUserManager is setup in Startup.Auth.cs

            // This is required for VeilSignInManager's constructor
            container.RegisterType<IAuthenticationManager>(
                new InjectionFactory(c => HttpContext.Current.GetOwinContext().Authentication));
        }
    }
}