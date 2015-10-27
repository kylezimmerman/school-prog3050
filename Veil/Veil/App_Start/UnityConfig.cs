using System;
using System.Data.Entity;
using System.Web;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.Owin.Security;
using Microsoft.Practices.Unity;
using Veil.DataAccess;
using Veil.DataAccess.Interfaces;
using Veil.DataModels.Models.Identity;
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

            // Used when resolving the UserStore constructor
            container.RegisterType<DbContext, VeilDataContext>(new HierarchicalLifetimeManager());

            container.RegisterType<VeilUserManager>(new HierarchicalLifetimeManager());
            container.RegisterType<VeilSignInManager>(new HierarchicalLifetimeManager());
            container.RegisterType<IStripeService, StripeService>();

            // Used by VeilUserManager
            container.RegisterType<IIdentityMessageService, EmailService>(new HierarchicalLifetimeManager());

            // Note: IDataProtectionProvider required by VeilUserManager is setup in Startup.Auth.cs

            // Create the UserStore for our identity types with the DbContext as IVeilDataAccess
            container.RegisterType<
                    IUserStore<User, Guid>,
                    UserStore<User, GuidIdentityRole, Guid, GuidIdentityUserLogin, GuidIdentityUserRole,
                            GuidIdentityUserClaim>
                >(new HierarchicalLifetimeManager());

            // This is required for VeilSignInManager's constructor
            container.RegisterType<IAuthenticationManager>(
                new InjectionFactory(c => HttpContext.Current.GetOwinContext().Authentication));
        }
    }
}