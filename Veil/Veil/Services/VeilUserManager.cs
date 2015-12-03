/* VeilUserManager.cs
 * Purpose: Subclass of UserManager using Veil's User class and Guid keys
 * 
 * Revision History:
 *      Drew Matheson, 2015.10.26: Created
 */ 

using System;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security.DataProtection;
using Veil.DataAccess.Interfaces;
using Veil.DataModels.Models.Identity;

namespace Veil.Services
{
    /// <summary>
    /// User Manager for the application
    /// </summary>
    [UsedImplicitly]
    public class VeilUserManager : UserManager<User, Guid>
    {
        /// <summary>
        ///     Instantiates a new instance of VeilUserManager with the provided arguments
        /// </summary>
        /// <param name="veilDataAccess">
        ///     The <see cref="IVeilDataAccess"/> to use for database access
        /// </param>
        /// <param name="emailService">
        ///     The <see cref="IIdentityMessageService"/> to use for sending emails
        /// </param>
        /// <param name="dataProtectionProvider">
        ///     The <see cref="IDataProtectionProvider"/> to use for generating tokens such as
        ///     password reset and email confirmation tokens
        /// </param>
        public VeilUserManager(
            IVeilDataAccess veilDataAccess, IIdentityMessageService emailService,
            IDataProtectionProvider dataProtectionProvider)
            : base(veilDataAccess.UserStore)
        {
            // Configure the application user manager used in this application. 

            // Configure validation logic for usernames
            UserValidator = new UserValidator<User, Guid>(this)
            {
                AllowOnlyAlphanumericUserNames = false,
                RequireUniqueEmail = true
            };

            // Configure validation logic for passwords
            PasswordValidator = new PasswordValidator
            {
                // TODO: Add these back when we are in release mode
                RequiredLength = 6,
#if !DEBUG
                RequireNonLetterOrDigit = true,
                RequireDigit = true,
                RequireLowercase = true,
                RequireUppercase = true,
#endif
            };

            // Configure user lockout defaults
            UserLockoutEnabledByDefault = true;
            DefaultAccountLockoutTimeSpan = TimeSpan.FromMinutes(15);
            MaxFailedAccessAttemptsBeforeLockout = 5;

            EmailService = emailService;

            if (dataProtectionProvider != null)
            {
                IDataProtector dataProtector = dataProtectionProvider.Create("ASP.NET Identity");

                var userTokenProvider = new DataProtectorTokenProvider<User, Guid>(dataProtector)
                {
                    TokenLifespan = TimeSpan.FromHours(12)
                };

                UserTokenProvider = userTokenProvider;
            }
        }

        // TODO: Rename this method to SendEmailAsync
        /// <summary>
        ///     Sends an email with the provided information
        /// </summary>
        /// <param name="address">
        ///     The email address to send the email to
        /// </param>
        /// <param name="title">
        ///     The subject line for the email
        /// </param>
        /// <param name="body">
        ///     The body for the email
        /// </param>
        /// <returns>
        ///     A task to await
        /// </returns>
        public virtual Task SendNewEmailConfirmationEmailAsync(string address, string title, string body)
        {
            IdentityMessage message = new IdentityMessage
            {
                Body = body,
                Subject = title,
                Destination = address
            };

            return EmailService.SendAsync(message);
        }
    }
}