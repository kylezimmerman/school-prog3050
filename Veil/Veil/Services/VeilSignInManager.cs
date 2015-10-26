using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;
using Veil.DataModels.Models.Identity;

namespace Veil.Services
{

    /// <summary>
    ///     Sign In Manager for the application
    /// </summary>
    public class VeilSignInManager : SignInManager<User, Guid>
    {
        public VeilSignInManager(VeilUserManager userManager, IAuthenticationManager authenticationManager)
            : base(userManager, authenticationManager)
        {
        }

        public override Task<ClaimsIdentity> CreateUserIdentityAsync(User user)
        {
            return user.GenerateUserIdentityAsync((VeilUserManager)UserManager);
        }
    }
}
