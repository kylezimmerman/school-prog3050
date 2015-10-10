using System;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;

namespace Veil.DataModels.Models.Identity
{
    // You can add profile data for the user by adding more properties to your User class, please visit http://go.microsoft.com/fwlink/?LinkID=317594 to learn more.
    public class User : IdentityUser<Guid, GuidIdentityUserLogin, GuidIdentityUserRole, GuidIdentityUserClaim>
    {
        public async Task<ClaimsIdentity> GenerateUserIdentityAsync(UserManager<User, Guid> manager)
        {
            // Note the authenticationType must match the one defined in CookieAuthenticationOptions.AuthenticationType
            var userIdentity = await manager.CreateIdentityAsync(this, DefaultAuthenticationTypes.ApplicationCookie);
            // Add custom user claims here
            return userIdentity;
        }

        /// <summary>
        /// The Person's first name
        /// </summary>
        [Required]
        [StringLength(maximumLength:64, MinimumLength = 1)]
        public string FirstName { get; set; }

        /// <summary>
        /// The Person's last name
        /// </summary>
        [Required]
        [StringLength(maximumLength: 64, MinimumLength = 1)]
        public string LastName { get; set; }

        public virtual Member Member { get; set; }

        public virtual Employee Employee { get; set; }
    }
}
