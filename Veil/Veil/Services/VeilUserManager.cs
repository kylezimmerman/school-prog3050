using System;
using JetBrains.Annotations;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security.DataProtection;
using Veil.DataModels.Models.Identity;

namespace Veil.Services
{
    /// <summary>
    /// User Manager for the application
    /// </summary>
    [UsedImplicitly]
    public class VeilUserManager : UserManager<User, Guid>
    {
        public VeilUserManager(IUserStore<User, Guid> store, IIdentityMessageService emailService, IDataProtectionProvider dataProtectionProvider)
            : base(store)
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
#if DEBUG
                RequiredLength = 6,
                RequireNonLetterOrDigit = true,
                RequireDigit = true,
                RequireLowercase = true,
                RequireUppercase = true,
#endif
            };

            // Configure user lockout defaults
            UserLockoutEnabledByDefault = true;
            DefaultAccountLockoutTimeSpan = TimeSpan.FromMinutes(5);
            MaxFailedAccessAttemptsBeforeLockout = 5;

            // Register two factor authentication providers. This application uses Phone and Emails as a step of receiving a code for verifying the user
            // You can write your own provider and plug it in here.
            base.RegisterTwoFactorProvider("Email Code", new EmailTokenProvider<User, Guid>
            {
                Subject = "Security Code",
                BodyFormat = "Your security code is {0}"
            });

            EmailService = emailService;

            if (dataProtectionProvider != null)
            {
                IDataProtector dataProtector = dataProtectionProvider.Create("ASP.NET Identity");

                UserTokenProvider = new DataProtectorTokenProvider<User, Guid>(dataProtector);
            }
        }
    }
}