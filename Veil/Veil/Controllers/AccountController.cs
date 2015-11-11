using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Transactions;
using System.Web;
using System.Web.Mvc;
using JetBrains.Annotations;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;
using Stripe;
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
        public const string LOGIN_MODEL_ERRORS_KEY = "loginModel";

        /* This must be kept in sync with the value used in _RegisterAccountParial */
        public const string REGISTER_MODEL_ERRORS_KEY = "registerModel";

        private readonly VeilSignInManager signInManager;
        private readonly VeilUserManager userManager;
        private readonly IStripeService stripeService;

        /// <summary>
        ///     Instantiates a new instance of AccountController
        /// </summary>
        /// <param name="userManager">The UserManager for the controller to use</param>
        /// <param name="signInManager">The SingInManager for the controller to use</param>
        /// <param name="stripeService">The IStripeService for the controller to use</param>
        public AccountController(VeilUserManager userManager, VeilSignInManager signInManager, IStripeService stripeService)
        {
            this.userManager = userManager;
            this.signInManager = signInManager;
            this.stripeService = stripeService;
        }

        /// <summary>
        ///     Displays the combined Login/Register page
        /// </summary>
        /// <param name="returnUrl">
        ///     The local url to return to if the user logs in
        /// </param>
        /// <returns>
        ///     The combined Login/Register page
        /// </returns>
        [AllowAnonymous]
        public ActionResult Login(string returnUrl)
        {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }

            ViewBag.ReturnUrl = returnUrl;
            return View(new LoginRegisterViewModel
            {
                LoginViewModel = new LoginViewModel(),
                RegisterViewModel = new RegisterViewModel()
            });
        }

        /// <summary>
        ///     Logs in the user with the supplied credentials and redirects them to the supplied url.
        ///     If the user's email isn't confirmed, they are shown the ConfirmResendConfirmationEmail view.
        /// 
        ///     This method also ensures the user is only correct roles as 
        ///     enforced by EnsureCorrectRolesAsync
        /// </summary>
        /// <param name="model">
        ///     The view model containing the log in credentials and a remember me boolean.
        /// </param>
        /// <param name="returnUrl">
        ///     The local URL to return to after logging in successfully.
        /// </param>
        /// <returns>
        ///     If successful, a redirection to the supplied local url.
        ///     If two factor auth is enable, a redirection to the SendCode action.
        ///     If login fails, the login page is reshown with a vague error message.
        ///     If the user's email isn't verified, the resend confirmation email page is shown.
        ///     If the user is locked out, the lockout shared view is shown. 
        /// </returns>
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

            User user = await userManager.FindByEmailAsync(model.LoginEmail);

            SignInStatus result = SignInStatus.Failure;

            if (user != null)
            {
                if (await userManager.CheckPasswordAsync(user, model.LoginPassword))
                {
                    // Require the user to have a confirmed email before they can log on.
                    if (!await userManager.IsEmailConfirmedAsync(user.Id))
                    {
                        return ConfirmResendConfirmationEmail(model.LoginEmail);
                    }

                    await EnsureCorrectRolesAsync(user);

                    // This doesn't count login failures towards account lockout
                    // To enable password failures to trigger account lockout, change to shouldLockout: true
                    result =
                        await signInManager.PasswordSignInAsync(
                                user.UserName, model.LoginPassword, model.RememberMe, shouldLockout: false);
                }
                else
                {
                    // This doesn't count login failures towards account lockout
                    // To enable password failures to trigger account lockout, uncomment this line
                    // NOTE: This returns an Identity result if you wish to do anything with it
                    // await userManager.AccessFailedAsync(user.Id);
                }
            }

            if (result == SignInStatus.Success && await userManager.IsInRoleAsync(user.Id, VeilRoles.MEMBER_ROLE))
            {
                // Set the Cart Quantity in the Session for use in the NavBar
                Session[CartController.CART_QTY_KEY] = user.Member.Cart.Items.Count;
            }

            switch (result)
            {
                case SignInStatus.Success:
                    return RedirectToLocal(returnUrl);
                case SignInStatus.LockedOut:
                    return View("Lockout");
                case SignInStatus.RequiresVerification:
                    // ReSharper disable once Mvc.ActionNotResolved TODO: Remove this if we don't add the SendCode action
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
        /// <param name="returnUrl">
        ///     The local url to return to if the user logs in
        /// </param>
        /// <remarks>
        ///     This is required because we don't redirect back to Login on failed postback to Register
        /// </remarks>
        [HttpGet]
        [AllowAnonymous]
        public ActionResult Register(string returnUrl)
        {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }

            ViewBag.ReturnUrl = returnUrl;

            var viewModel = new LoginRegisterViewModel
            {
                LoginViewModel = new LoginViewModel(),
                RegisterViewModel = new RegisterViewModel()
            };

            return View("Login", viewModel);
        }

        /// <summary>
        ///     Registers a user account with the provided information if it is valid
        /// </summary>
        /// <param name="model">
        ///     The view model containing the information to validate and create a new account for
        /// </param>
        /// <param name="returnUrl">
        ///     The local url to return to if the user logs in
        /// </param>
        /// <returns>
        ///     If successful, displays the RegisterComplete page. 
        ///     Otherwise, redisplays the login page with errors.
        /// </returns>
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Register(RegisterViewModel model, string returnUrl)
        {
            LoginRegisterViewModel viewModel;
            bool stripeCustomerScopeSuccessful = false;

            if (ModelState.IsValid)
            {
                var user = new User
                {
                    UserName = model.Username,
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
                            if (ex.HttpStatusCode >= HttpStatusCode.InternalServerError 
                                || (int)ex.HttpStatusCode == 429 /* Too Many Requests */ 
                                || (int)ex.HttpStatusCode == 402 /* Request Failed */)
                            {
                                ModelState.AddModelError(REGISTER_MODEL_ERRORS_KEY,
                                    "An error occured while creating a customer account for you. Please try again later.");
                            }
                            else if (ex.HttpStatusCode == HttpStatusCode.Unauthorized)
                            {
                                // TODO: We want to log this as it means we don't have a valid API key
                                Debug.WriteLine("Stripe API Key is Invalid");
                                Debug.WriteLine(ex.Message);

                                return new HttpStatusCodeResult(HttpStatusCode.InternalServerError);
                            }
                            else
                            {
                                // TODO: Log unknown errors
                                Debug.WriteLine(ex.HttpStatusCode);
                                Debug.WriteLine($"Stripe Type: {ex.StripeError.ErrorType}");
                                Debug.WriteLine($"Stripe Message: {ex.StripeError.Message}");
                                Debug.WriteLine($"Stripe Code: {ex.StripeError.Code}");
                                Debug.WriteLine($"Stripe Param: {ex.StripeError.Parameter}");

                                ModelState.AddModelError(REGISTER_MODEL_ERRORS_KEY, 
                                    "An unknown error occured while creating a customer account for you. Please try again later.");
                            }

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
                    result = await userManager.AddToRoleAsync(user.Id, VeilRoles.MEMBER_ROLE);

                    if (result.Succeeded)
                    {
                        await SendConfirmationEmail(user);

                        return View("RegisterComplete");
                    }
                }

                AddErrors(result, REGISTER_MODEL_ERRORS_KEY);
            }

            ViewBag.ReturnUrl = returnUrl;

            viewModel = new LoginRegisterViewModel
            {
                LoginViewModel = new LoginViewModel(),
                RegisterViewModel = model
            };

            // If we got this far, something failed, redisplay form
            return View("Login", viewModel);
        }

        /// <summary>
        ///     Allows the user to resend an email confirmation link to them.
        /// </summary>
        /// <param name="emailAddress">
        ///     The email address to be used if the user choose to resend the link
        /// </param>
        /// <returns>
        ///     A view allowing the user to resend a email confirmation link
        /// </returns>
        /// <remarks>
        ///     This action can not be accessed directly via Url.
        /// </remarks>
        [ChildActionOnly]
        public ActionResult ConfirmResendConfirmationEmail(string emailAddress)
        {
            if (string.IsNullOrWhiteSpace(emailAddress))
            {
                return View("Error");
            }

            // Need to cast the email to object otherwise the method 
            // interprets it as masterName instead of model
            return View("ConfirmResendConfirmationEmail", (object)emailAddress);
        }

        /// <summary>
        ///     Sends an email confirmation link to the specified email address
        ///     if it belongs to a user of the site.
        /// </summary>
        /// <param name="emailAddress">
        ///     The email address to send the confirmation link to
        /// </param>
        /// <returns>
        ///     A view informing the user that a confirmation email was 
        ///     re-sent regardless of if one actually was.
        /// </returns>
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ResendConfirmationEmail(string emailAddress)
        {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }

            User user = await userManager.FindByEmailAsync(emailAddress);

            if (user != null)
            {
                await SendConfirmationEmail(user);
            }

            return View("ResendConfimationEmail");
        }

        /// <summary>
        ///     Validates the confirmation info and sets the user's email as confirmed
        /// </summary>
        /// <param name="userId">
        ///     The Id of the user
        /// </param>
        /// <param name="code">
        ///     The validation code
        /// </param>
        /// <returns>
        ///     If successful, a view letting the user know their email has confirmed.
        ///      Otherwise, an error view.
        /// </returns>
        [AllowAnonymous]
        public async Task<ActionResult> ConfirmEmail(Guid userId, string code)
        {
            if (userId == Guid.Empty || string.IsNullOrWhiteSpace(code))
            {
                return View("Error");
            }

            var result = await userManager.ConfirmEmailAsync(userId, code);

            if (!result.Succeeded)
            {
                AddErrors(result, "");

                return View("Error");
            }

            // Update the security stamp to invalidate the email link
            await userManager.UpdateSecurityStampAsync(userId);

            return View("ConfirmEmail");
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

        /// <summary>
        ///     Logs the user out
        /// </summary>
        /// <returns>
        ///     The home page
        /// </returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult LogOff()
        {
            signInManager.AuthenticationManager.SignOut(DefaultAuthenticationTypes.ApplicationCookie);

            return RedirectToAction("Index", "Home");
        }

        #region Currently Unused/Unimplemented TODO: remove anything remaining here at project end
        /*
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
        */
        #endregion

        /// <summary>
        ///     Ensures the user is only in the member or employee roles if they are a member or an employee
        /// </summary>
        /// <param name="user">
        ///     The user
        /// </param>
        /// <returns>
        ///     True if roles were modified, false otherwise
        /// </returns>
        private async Task<bool> EnsureCorrectRolesAsync([NotNull]User user)
        {
            bool rolesChanged = false;

            bool isInMemberRole = await userManager.IsInRoleAsync(user.Id, VeilRoles.MEMBER_ROLE);
            bool isInEmployeeRole = await userManager.IsInRoleAsync(user.Id, VeilRoles.EMPLOYEE_ROLE);

            if (!isInMemberRole && user.Member != null)
            {
                await userManager.AddToRoleAsync(user.Id, VeilRoles.MEMBER_ROLE);
                rolesChanged = true;
            }
            else if (isInMemberRole && user.Member == null)
            {
                await userManager.RemoveFromRoleAsync(user.Id, VeilRoles.MEMBER_ROLE);
                rolesChanged = true;
            }

            if (!isInEmployeeRole && user.Employee != null)
            {
                await userManager.AddToRoleAsync(user.Id, VeilRoles.EMPLOYEE_ROLE);
                rolesChanged = true;
            }
            else if (isInEmployeeRole && user.Member == null)
            {
                await userManager.RemoveFromRoleAsync(user.Id, VeilRoles.EMPLOYEE_ROLE);
                rolesChanged = true;
            }

            if (rolesChanged)
            {
                await userManager.UpdateSecurityStampAsync(user.Id);
            }

            return rolesChanged;
        }

        /// <summary>
        ///     Sends a confirmation to the user's email address
        /// </summary>
        /// <param name="user">
        ///     The user to send an email confirmation email to
        /// </param>
        /// <returns>
        ///     The awaitable task for sending the email
        /// </returns>
        private async Task SendConfirmationEmail(User user)
        {
            // For more information on how to enable account confirmation and password reset please visit http://go.microsoft.com/fwlink/?LinkID=320771
            // Send an email with this link
            string code = await userManager.GenerateEmailConfirmationTokenAsync(user.Id);
            var callbackUrl = Url.Action("ConfirmEmail", "Account", 
                new
                {
                    userId = user.Id,
                    code = code
                },
                protocol: Request.Url.Scheme);

            await userManager.SendEmailAsync(user.Id,
                "Veil - Please Confirm Your Account",
                "<h1>Welcome to Veil!</h1>" +
                "Please confirm your account by clicking <a href=\"" + callbackUrl + "\">here</a>");
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