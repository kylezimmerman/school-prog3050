using System;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;
using Stripe;
using Veil.DataAccess.Interfaces;
using Veil.DataModels;
using Veil.DataModels.Models;
using Veil.DataModels.Models.Identity;
using Veil.Models;
using Veil.Services;
using Veil.Services.Interfaces;

namespace Veil.Controllers
{
    [Authorize]
    public class AccountController : Controller
    {
        /* This must be kept in sync with the value used in _LoginAccountPartial */
        private const string LOGIN_MODEL_ERRORS_KEY = "loginModel";

        /* This must be kept in sync with the value used in _RegisterAccountParial */
        private const string REGISTER_MODEL_ERRORS_KEY = "registerModel";

        private readonly VeilSignInManager signInManager;
        private readonly VeilUserManager userManager;
        private readonly IStripeService stripeService;
        private readonly IVeilDataAccess db;

        public AccountController(VeilUserManager userManager, VeilSignInManager signInManager, IStripeService stripeService, IVeilDataAccess veilDataAccess)
        {
            this.userManager = userManager;
            this.signInManager = signInManager;
            this.stripeService = stripeService;
            db = veilDataAccess;
        }

        //
        // GET: /Account/Login
        [AllowAnonymous]
        public ActionResult Login(string returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View(new LoginRegisterViewModel
            {
                LoginViewModel = new LoginViewModel(),
                RegisterViewModel = new RegisterViewModel()
            });
        }

        //
        // POST: /Account/Login
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Login(LoginViewModel model, string returnUrl)
        {
            if (!ModelState.IsValid)
            {
                LoginRegisterViewModel viewModel = new LoginRegisterViewModel
                {
                    LoginViewModel = model,
                    RegisterViewModel = new RegisterViewModel()
                };

                return View(viewModel);
            }

            // This doesn't count login failures towards account lockout
            // To enable password failures to trigger account lockout, change to shouldLockout: true
            SignInStatus result = await signInManager.PasswordSignInAsync(model.LoginEmail, model.LoginPassword, model.RememberMe, shouldLockout: false);

            if (result == SignInStatus.Success && await EnsureCorrectRolesAsync(model.LoginEmail))
            {
                /* TODO: This is an ugly hack to make the added roles be in immediate effect.
                   I'm not sure of a better way to accomplish this */
                signInManager.AuthenticationManager.SignOut(DefaultAuthenticationTypes.ApplicationCookie);

                await
                    signInManager.PasswordSignInAsync(
                        model.LoginEmail, model.LoginPassword, model.RememberMe, shouldLockout: false);
            }

            switch (result)
            {
                case SignInStatus.Success:
                    return RedirectToLocal(returnUrl);
                case SignInStatus.LockedOut:
                    return View("Lockout");
                case SignInStatus.RequiresVerification:
                    return RedirectToAction("SendCode", new { ReturnUrl = returnUrl, RememberMe = model.RememberMe });
                case SignInStatus.Failure:
                default:
                    ModelState.AddModelError(LOGIN_MODEL_ERRORS_KEY, "Invalid login attempt.");

                    LoginRegisterViewModel viewModel = new LoginRegisterViewModel
                    {
                        LoginViewModel = model,
                        RegisterViewModel = new RegisterViewModel()
                    };

                    return View(viewModel);
            }
        }

        /// <summary>
        ///     Redisplays the Login View
        /// </summary>
        /// <remarks>
        ///     This is required because we don't redirect back to Login on failed postback to Register
        /// </remarks>
        [HttpGet]
        [AllowAnonymous]
        public ActionResult Register()
        {
            var viewModel = new LoginRegisterViewModel
            {
                LoginViewModel = new LoginViewModel(),
                RegisterViewModel = new RegisterViewModel()
            };

            return View("Login", viewModel);
        }

        //
        // POST: /Account/Register
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Register(RegisterViewModel model)
        {
            LoginRegisterViewModel viewModel;
            bool stripeCustomerScopeSuccessful = false;

            if (ModelState.IsValid)
            {
                var user = new User
                {
                    UserName = model.Email,
                    Email = model.Email,
                    FirstName = model.FirstName,
                    LastName = model.LastName
                };

                IdentityResult result;

                // We need a transaction as we don't want to create the User if we fail to create and
                // save a Stripe customer for them
                using (TransactionScope stripeCustomerScope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
                {
                    result = await userManager.CreateAsync(user, model.Password);

                    if (result.Succeeded)
                    {
                        string stripeCustomerId;

                        try
                        {
                            stripeCustomerId = stripeService.CreateCustomer(user);
                        }
                        catch (StripeException ex)
                        {
                            // TODO: We probably don't want to not show users the raw error message
                            ModelState.AddModelError(REGISTER_MODEL_ERRORS_KEY, $"Stripe Error: {ex.Message}");

                            viewModel = new LoginRegisterViewModel
                            {
                                LoginViewModel = new LoginViewModel(),
                                RegisterViewModel = model
                            };

                            return View("Login", viewModel);
                        }

                        user.Member = new Member
                        {
                            ReceivePromotionalEmails = model.ReceivePromotionalEmail,
                            WishListVisibility = model.WishListVisibility,
                            StripeCustomerId = stripeCustomerId
                        };

                        result = await userManager.UpdateAsync(user);

                        if (result.Succeeded)
                        {
                            // Commit the transaction as we successfully created the Stripe customer and
                            // commited that information in a Member for the user.
                            stripeCustomerScope.Complete();

                            stripeCustomerScopeSuccessful = true;
                        }
                    }
                }

                // We don't need this portion to be inside stripeCustomerScope 
                if (result.Succeeded && stripeCustomerScopeSuccessful)
                {
                    result = await userManager.AddToRoleAsync(user.Id, VeilRoles.MemberRole);

                    if (result.Succeeded)
                    {
                        await signInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);

                        // For more information on how to enable account confirmation and password reset please visit http://go.microsoft.com/fwlink/?LinkID=320771
                        // Send an email with this link
                        // string code = await UserManager.GenerateEmailConfirmationTokenAsync(user.Id);
                        // var callbackUrl = Url.Action("ConfirmEmail", "Account", new { userId = user.Id, code = code }, protocol: Request.Url.Scheme);
                        // await UserManager.SendEmailAsync(user.Id, "Confirm your account", "Please confirm your account by clicking <a href=\"" + callbackUrl + "\">here</a>");

                        return RedirectToAction("Index", "Home");
                    }
                }

                AddErrors(result, REGISTER_MODEL_ERRORS_KEY);
            }

            viewModel = new LoginRegisterViewModel
            {
                LoginViewModel = new LoginViewModel(),
                RegisterViewModel = model
            };

            // If we got this far, something failed, redisplay form
            return View("Login", viewModel);
        }

        //
        // GET: /Account/ConfirmEmail
        [AllowAnonymous]
        public async Task<ActionResult> ConfirmEmail(Guid userId, string code)
        {
            if (userId == Guid.Empty || code == null)
            {
                return View("Error");
            }

            var result = await userManager.ConfirmEmailAsync(userId, code);

            return View(result.Succeeded ? "ConfirmEmail" : "Error");
        }

        //
        // GET: /Account/ForgotPassword
        [AllowAnonymous]
        public ActionResult ForgotPassword()
        {
            return View();
        }

        //
        // POST: /Account/ForgotPassword
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await userManager.FindByNameAsync(model.Email);
                if (user == null || !(await userManager.IsEmailConfirmedAsync(user.Id)))
                {
                    // Don't reveal that the user does not exist or is not confirmed
                    return View("ForgotPasswordConfirmation");
                }

                // For more information on how to enable account confirmation and password reset please visit http://go.microsoft.com/fwlink/?LinkID=320771
                // Send an email with this link
                // string code = await UserManager.GeneratePasswordResetTokenAsync(user.Id);
                // var callbackUrl = Url.Action("ResetPassword", "Account", new { userId = user.Id, code = code }, protocol: Request.Url.Scheme);		
                // await UserManager.SendEmailAsync(user.Id, "Reset Password", "Please reset your password by clicking <a href=\"" + callbackUrl + "\">here</a>");
                // return RedirectToAction("ForgotPasswordConfirmation", "Account");
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        //
        // GET: /Account/ForgotPasswordConfirmation
        [AllowAnonymous]
        public ActionResult ForgotPasswordConfirmation()
        {
            return View();
        }

        //
        // GET: /Account/ResetPassword
        [AllowAnonymous]
        public ActionResult ResetPassword(string code)
        {
            return code == null ? View("Error") : View();
        }

        //
        // POST: /Account/ResetPassword
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            var user = await userManager.FindByNameAsync(model.Email);
            if (user == null)
            {
                // Don't reveal that the user does not exist
                return RedirectToAction("ResetPasswordConfirmation", "Account");
            }
            var result = await userManager.ResetPasswordAsync(user.Id, model.Code, model.Password);
            if (result.Succeeded)
            {
                return RedirectToAction("ResetPasswordConfirmation", "Account");
            }
            AddErrors(result, "");
            return View();
        }

        //
        // GET: /Account/ResetPasswordConfirmation
        [AllowAnonymous]
        public ActionResult ResetPasswordConfirmation()
        {
            return View();
        }

        //
        // POST: /Account/LogOff
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult LogOff()
        {
            signInManager.AuthenticationManager.SignOut(DefaultAuthenticationTypes.ApplicationCookie);
            return RedirectToAction("Index", "Home");
        }

        #region Currently Unused/Unimplemented TODO: remove anything remaining here at project end

        //
        // GET: /Account/VerifyCode
        [AllowAnonymous]
        public async Task<ActionResult> VerifyCode(string provider, string returnUrl, bool rememberMe)
        {
            // Require that the user has already logged in via username/password or external login
            if (!await signInManager.HasBeenVerifiedAsync())
            {
                return View("Error");
            }
            return View(new VerifyCodeViewModel { Provider = provider, ReturnUrl = returnUrl, RememberMe = rememberMe });
        }

        //
        // POST: /Account/VerifyCode
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> VerifyCode(VerifyCodeViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // The following code protects for brute force attacks against the two factor codes. 
            // If a user enters incorrect codes for a specified amount of time then the user account 
            // will be locked out for a specified amount of time. 
            // You can configure the account lockout settings in IdentityConfig
            var result = await signInManager.TwoFactorSignInAsync(model.Provider, model.Code, isPersistent: model.RememberMe, rememberBrowser: model.RememberBrowser);
            switch (result)
            {
                case SignInStatus.Success:
                    return RedirectToLocal(model.ReturnUrl);
                case SignInStatus.LockedOut:
                    return View("Lockout");
                case SignInStatus.Failure:
                default:
                    ModelState.AddModelError("", "Invalid code.");
                    return View(model);
            }
        }

        //
        // POST: /Account/ExternalLogin
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult ExternalLogin(string provider, string returnUrl)
        {
            // Request a redirect to the external login provider
            return new ChallengeResult(provider, Url.Action("ExternalLoginCallback", "Account", new
            {
                ReturnUrl = returnUrl
            }));
        }

        //
        // GET: /Account/SendCode
        [AllowAnonymous]
        public async Task<ActionResult> SendCode(string returnUrl, bool rememberMe)
        {
            var userId = await signInManager.GetVerifiedUserIdAsync();
            if (userId == Guid.Empty)
            {
                return View("Error");
            }
            var userFactors = await userManager.GetValidTwoFactorProvidersAsync(userId);
            var factorOptions = userFactors.Select(purpose => new SelectListItem { Text = purpose, Value = purpose }).ToList();
            return View(new SendCodeViewModel { Providers = factorOptions, ReturnUrl = returnUrl, RememberMe = rememberMe });
        }

        //
        // POST: /Account/SendCode
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> SendCode(SendCodeViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View();
            }

            // Generate the token and send it
            if (!await signInManager.SendTwoFactorCodeAsync(model.SelectedProvider))
            {
                return View("Error");
            }
            return RedirectToAction("VerifyCode", new
            {
                Provider = model.SelectedProvider,
                ReturnUrl = model.ReturnUrl,
                RememberMe = model.RememberMe
            });
        }

        //
        // GET: /Account/ExternalLoginCallback
        [AllowAnonymous]
        public async Task<ActionResult> ExternalLoginCallback(string returnUrl)
        {
            var loginInfo = await signInManager.AuthenticationManager.GetExternalLoginInfoAsync();
            if (loginInfo == null)
            {
                return RedirectToAction("Login");
            }

            // Sign in the user with this external login provider if the user already has a login
            var result = await signInManager.ExternalSignInAsync(loginInfo, isPersistent: false);
            switch (result)
            {
                case SignInStatus.Success:
                    return RedirectToLocal(returnUrl);
                case SignInStatus.LockedOut:
                    return View("Lockout");
                case SignInStatus.RequiresVerification:
                    return RedirectToAction("SendCode", new
                    {
                        ReturnUrl = returnUrl,
                        RememberMe = false
                    });
                case SignInStatus.Failure:
                default:
                    // If the user does not have an account, then prompt the user to create an account
                    ViewBag.ReturnUrl = returnUrl;
                    ViewBag.LoginProvider = loginInfo.Login.LoginProvider;
                    return View("ExternalLoginConfirmation", new ExternalLoginConfirmationViewModel { Email = loginInfo.Email });
            }
        }

        //
        // POST: /Account/ExternalLoginConfirmation
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ExternalLoginConfirmation(ExternalLoginConfirmationViewModel model, string returnUrl)
        {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Manage");
            }

            if (ModelState.IsValid)
            {
                // Get the information about the user from the external login provider
                var info = await signInManager.AuthenticationManager.GetExternalLoginInfoAsync();
                if (info == null)
                {
                    return View("ExternalLoginFailure");
                }
                var user = new User { UserName = model.Email, Email = model.Email };
                var result = await userManager.CreateAsync(user);
                if (result.Succeeded)
                {
                    result = await userManager.AddLoginAsync(user.Id, info.Login);
                    if (result.Succeeded)
                    {
                        await signInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
                        return RedirectToLocal(returnUrl);
                    }
                }
                AddErrors(result, "");
            }

            ViewBag.ReturnUrl = returnUrl;
            return View(model);
        }

        //
        // GET: /Account/ExternalLoginFailure
        [AllowAnonymous]
        public ActionResult ExternalLoginFailure()
        {
            return View();
        }
        #endregion

        /// <summary>
        ///     Ensures the user is only in the member or employee roles if they are a member or an employee
        /// </summary>
        /// <param name="loginEmail">
        ///     The email address for the user
        /// </param>
        /// <returns>
        ///     True if roles were modified, false otherwise
        /// </returns>
        private async Task<bool> EnsureCorrectRolesAsync(string loginEmail)
        {
            bool rolesChanged = false;

            var userInfo = await db.Users.
                Where(u => u.Email == loginEmail).
                Select(u => new
                {
                    Id = u.Id,
                    IsMember = u.Member != null,
                    IsEmployee = u.Employee != null
                }).
                FirstOrDefaultAsync();

            if (userInfo == null)
            {
                return false;
            }


            bool isInMemberRole = await userManager.IsInRoleAsync(userInfo.Id, VeilRoles.MemberRole);
            bool isInEmployeeRole = await userManager.IsInRoleAsync(userInfo.Id, VeilRoles.EmployeeRole);

            if (!isInMemberRole && userInfo.IsMember)
            {
                await userManager.AddToRoleAsync(userInfo.Id, VeilRoles.MemberRole);
                rolesChanged = true;
            }
            else if (isInMemberRole && !userInfo.IsMember)
            {
                await userManager.RemoveFromRoleAsync(userInfo.Id, VeilRoles.MemberRole);
                rolesChanged = true;
            }

            if (!isInEmployeeRole && userInfo.IsEmployee)
            {
                await userManager.AddToRoleAsync(userInfo.Id, VeilRoles.EmployeeRole);
                rolesChanged = true;
            }
            else if (isInEmployeeRole && !userInfo.IsEmployee)
            {
                await userManager.RemoveFromRoleAsync(userInfo.Id, VeilRoles.EmployeeRole);
                rolesChanged = true;
            }

            if (rolesChanged)
            {
                await userManager.UpdateSecurityStampAsync(userInfo.Id);
            }

            return rolesChanged;
        }

        #region Helpers
        // Used for XSRF protection when adding external logins
        private const string XsrfKey = "XsrfId";

        private void AddErrors(IdentityResult result, string tag)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(tag, error);
            }
        }

        private ActionResult RedirectToLocal(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction("Index", "Home");
        }

        internal class ChallengeResult : HttpUnauthorizedResult
        {
            public ChallengeResult(string provider, string redirectUri)
                : this(provider, redirectUri, null)
            {
            }

            public ChallengeResult(string provider, string redirectUri, string userId)
            {
                LoginProvider = provider;
                RedirectUri = redirectUri;
                UserId = userId;
            }

            public string LoginProvider { get; set; }
            public string RedirectUri { get; set; }
            public string UserId { get; set; }

            public override void ExecuteResult(ControllerContext context)
            {
                var properties = new AuthenticationProperties { RedirectUri = RedirectUri };
                if (UserId != null)
                {
                    properties.Dictionary[XsrfKey] = UserId;
                }
                context.HttpContext.GetOwinContext().Authentication.Challenge(properties, LoginProvider);
            }
        }
        #endregion
    }
}