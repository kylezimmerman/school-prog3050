/* VeilSignInManager.cs
 * Purpose: Subclass of SignInManager using Veil's User class and Guid keys
 * 
 * Revision History:
 *      Drew Matheson, 2015.10.26: Created
 */ 

using System;
using System.Security.Claims;
using System.Threading.Tasks;
using JetBrains.Annotations;
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
        /// <summary>
        ///     Instantiates a new instance of VeilSignInManager with the provided arguments
        /// </summary>
        /// <param name="userManager"></param>
        /// <param name="authenticationManager"></param>
        [UsedImplicitly]
        public VeilSignInManager(VeilUserManager userManager, IAuthenticationManager authenticationManager)
            : base(userManager, authenticationManager) { }

        /// <summary>
        ///     Called to generate the ClaimsIdentity for the user, override to add 
        ///     additional claims before SignIn
        /// </summary>
        /// <param name="user">
        ///     The <see cref="User"/> to generate a user identity for
        /// </param>
        /// <returns>
        ///     A task to await which will resolve to the ClaimsIdentity
        /// </returns>
        public override Task<ClaimsIdentity> CreateUserIdentityAsync(User user)
        {
            return user.GenerateUserIdentityAsync((VeilUserManager) UserManager);
        }
    }
}