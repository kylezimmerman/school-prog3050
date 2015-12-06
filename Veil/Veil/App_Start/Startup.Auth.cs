/* Startup.Auth.cs
 * Purpose: Partial Startup class used to configure authentication
 * 
 * Revision History:
 *      Drew Matheson, 2015.09.25: Created
 */ 

using System;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.DataProtection;
using Microsoft.Practices.Unity;
using Owin;
using Veil.DataModels.Models.Identity;
using Veil.Helpers;
using Veil.Services;

namespace Veil
{
    /// <summary>
    ///     Partial class of Startup which contains auth configuration
    /// </summary>
    public partial class Startup
    {
        // For more information on configuring authentication, please visit http://go.microsoft.com/fwlink/?LinkId=301864
        /// <summary>
        ///     Configures the authentication used by Veil
        /// </summary>
        /// <param name="app">
        ///     The <see cref="IAppBuilder"/> to configure
        /// </param>
        public void ConfigureAuth(IAppBuilder app)
        {
            // Setup Unity to Configure IDataProtectionProvider for the VeilUserManager constructor
            UnityConfig.GetConfiguredContainer().RegisterInstance(app.GetDataProtectionProvider());

            IGuidUserIdGetter idGetter = UnityConfig.GetConfiguredContainer().Resolve<IGuidUserIdGetter>();

            // Enable the application to use a cookie to store information for the signed in user
            // and to use a cookie to temporarily store information about a user logging in with a third party login provider
            // Configure the sign in cookie
            app.UseCookieAuthentication(
                new CookieAuthenticationOptions
                {
                    AuthenticationType = DefaultAuthenticationTypes.ApplicationCookie,
                    LoginPath = new PathString("/Account/Login"),
                    ReturnUrlParameter = "returnUrl",
                    Provider = new CookieAuthenticationProvider
                    {
                        // Enables the application to validate the security stamp when the user logs in.
                        // This is a security feature which is used when you change a password or add an external login to your account.  
                        OnValidateIdentity =
                            SecurityStampValidator.OnValidateIdentity<VeilUserManager, User, Guid>(
                                validateInterval: TimeSpan.FromMinutes(10),
                                regenerateIdentityCallback:
                                    (manager, user) => user.GenerateUserIdentityAsync(manager),
                                getUserIdCallback: identity => idGetter.GetUserId(identity))
                    }
                });
        }
    }
}