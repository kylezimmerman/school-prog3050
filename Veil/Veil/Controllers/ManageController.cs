using System;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Microsoft.Owin.Security;
using Stripe;
using Veil.DataAccess.Interfaces;
using Veil.DataModels;
using Veil.DataModels.Models;
using Veil.DataModels.Validation;
using Veil.Helpers;
using Veil.Models;
using Veil.Services;
using Veil.Services.Interfaces;

namespace Veil.Controllers
{
    [Authorize(Roles = VeilRoles.MEMBER_ROLE)]
    public class ManageController : BaseController
    {
        public const string STRIPE_ISSUES_MODELSTATE_KEY = "StripeIssues";

        private readonly VeilSignInManager signInManager;
        private readonly VeilUserManager userManager;
        private readonly IVeilDataAccess db;
        private readonly IGuidUserIdGetter idGetter;
        private readonly IStripeService stripeService;

        private static readonly Regex postalCodeRegex = new Regex(ValidationRegex.POSTAL_CODE, RegexOptions.Compiled);
        private static readonly Regex zipCodeRegex = new Regex(ValidationRegex.ZIP_CODE, RegexOptions.Compiled);

        public ManageController(VeilUserManager userManager, VeilSignInManager signInManager, IVeilDataAccess veilDataAccess, IGuidUserIdGetter idGetter, IStripeService stripeService)
        {
            this.userManager = userManager;
            this.signInManager = signInManager;
            db = veilDataAccess;
            this.idGetter = idGetter;
            this.stripeService = stripeService;
        }

        //
        // GET: /Manage/Index
        public async Task<ActionResult> Index(ManageMessageId? message)
        {
            string statusMessage;

            switch (message)
            {
                case ManageMessageId.AddPhoneSuccess:
                    statusMessage = "Your phone number was added.";
                    break;
                case ManageMessageId.ChangePasswordSuccess:
                    statusMessage = "Your password has been changed.";
                    break;
                case ManageMessageId.SetTwoFactorSuccess:
                    statusMessage = "Your two-factor authentication provider has been set.";
                    break;
                case ManageMessageId.SetPasswordSuccess:
                    statusMessage = "Your password has been set.";
                    break;
                case ManageMessageId.RemoveLoginSuccess:
                    statusMessage = "A login has been removed.";
                    break;
                case ManageMessageId.RemovePhoneSuccess:
                    statusMessage = "Your phone number was removed.";
                    break;
                case ManageMessageId.Error:
                    statusMessage = "An error has occurred.";
                    break;
                default:
                    statusMessage = "";
                    break;
            }

            var userId = GetUserId();

            string phoneNumber;

            try
            {
                phoneNumber = await userManager.GetPhoneNumberAsync(userId);
            }
            catch (InvalidOperationException)
            {
                // If this happens, the user has been deleted in the database but still has a valid login cookie
                signInManager.AuthenticationManager.SignOut(DefaultAuthenticationTypes.ApplicationCookie);

                return RedirectToAction("Index", "Home");
            }
            
            var model = new IndexViewModel
            {
                HasPassword = HasPassword(),
                PhoneNumber = phoneNumber,
                TwoFactor = await userManager.GetTwoFactorEnabledAsync(userId),
                Logins = await userManager.GetLoginsAsync(userId),
                BrowserRemembered = await signInManager.AuthenticationManager.TwoFactorBrowserRememberedAsync(userId.ToString()),
                StatusMessage = statusMessage
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> UpdateProfile(IndexViewModel viewModel)
        {
            ManageMessageId? message = null;

            return RedirectToAction("Index", new { Message = message });
        }

        //
        // POST: /Manage/RemoveLogin
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> RemoveLogin(string loginProvider, string providerKey)
        {
            ManageMessageId? message;
            var result = await userManager.RemoveLoginAsync(GetUserId(), new UserLoginInfo(loginProvider, providerKey));
            if (result.Succeeded)
            {
                var user = await userManager.FindByIdAsync(GetUserId());
                if (user != null)
                {
                    await signInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
                }
                message = ManageMessageId.RemoveLoginSuccess;
            }
            else
            {
                message = ManageMessageId.Error;
            }
            return RedirectToAction("ManageLogins", new { Message = message });
        }

        //
        // POST: /Manage/EnableTwoFactorAuthentication
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> EnableTwoFactorAuthentication()
        {
            await userManager.SetTwoFactorEnabledAsync(GetUserId(), true);
            var user = await userManager.FindByIdAsync(GetUserId());
            if (user != null)
            {
                await signInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
            }
            return RedirectToAction("Index", "Manage");
        }

        //
        // POST: /Manage/DisableTwoFactorAuthentication
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DisableTwoFactorAuthentication()
        {
            await userManager.SetTwoFactorEnabledAsync(GetUserId(), false);
            var user = await userManager.FindByIdAsync(GetUserId());
            if (user != null)
            {
                await signInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
            }
            return RedirectToAction("Index", "Manage");
        }

        //
        // GET: /Manage/ChangePassword
        public ActionResult ChangePassword()
        {
            return View();
        }

        //
        // POST: /Manage/ChangePassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            var result = await userManager.ChangePasswordAsync(GetUserId(), model.OldPassword, model.NewPassword);
            if (result.Succeeded)
            {
                var user = await userManager.FindByIdAsync(GetUserId());
                if (user != null)
                {
                    await signInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
                }
                return RedirectToAction("Index", new { Message = ManageMessageId.ChangePasswordSuccess });
            }
            AddErrors(result);
            return View(model);
        }

        //
        // GET: /Manage/SetPassword
        public ActionResult SetPassword()
        {
            return View();
        }

        //
        // POST: /Manage/SetPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> SetPassword(SetPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var result = await userManager.AddPasswordAsync(GetUserId(), model.NewPassword);
                if (result.Succeeded)
                {
                    var user = await userManager.FindByIdAsync(GetUserId());
                    if (user != null)
                    {
                        await signInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
                    }
                    return RedirectToAction("Index", new { Message = ManageMessageId.SetPasswordSuccess });
                }
                AddErrors(result);
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        //
        // GET: /Manage/ManageLogins
        public async Task<ActionResult> ManageLogins(ManageMessageId? message)
        {
            ViewBag.StatusMessage = message == ManageMessageId.RemoveLoginSuccess ? "The external login was removed." : message == ManageMessageId.Error ? "An error has occurred." : "";

            var user = await userManager.FindByIdAsync(GetUserId());
            if (user == null)
            {
                return View("Error");
            }
            var userLogins = await userManager.GetLoginsAsync(GetUserId());
            var otherLogins = signInManager.AuthenticationManager.GetExternalAuthenticationTypes().Where(auth => userLogins.All(ul => auth.AuthenticationType != ul.LoginProvider)).ToList();
            ViewBag.ShowRemoveButton = user.PasswordHash != null || userLogins.Count > 1;
            return View(new ManageLoginsViewModel
            {
                CurrentLogins = userLogins, OtherLogins = otherLogins
            });
        }

        /// <summary>
        ///     Displays a view for adding or removing addresses
        /// </summary>
        /// <returns>
        ///     The view.
        /// </returns>
        public async Task<ActionResult> ManageAddresses()
        {
            ManageAddressViewModel model = new ManageAddressViewModel();

            await SetupAddressesAndCountries(model);

            return View(model);
        }

        /// <summary>
        ///     Creates a new address for the member
        /// </summary>
        /// <param name="model">
        ///     <see cref="ManageAddressViewModel"/> containing the address details
        /// </param>
        /// <returns>
        ///     Redirects back to ManageAddresses if successful
        ///     Redisplays the form if the information is invalid or a database error occurs
        /// </returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> CreateAddress(
            [Bind(Exclude = nameof(ManageAddressViewModel.Countries) + "," + nameof(ManageAddressViewModel.Addresses))]ManageAddressViewModel model)
        {
            if (!ModelState.IsValidField(nameof(model.PostalCode)) && !string.IsNullOrWhiteSpace(model.CountryCode))
            {
                // Remove the default validation message to provide a more specific one.
                ModelState.Remove(nameof(model.PostalCode));

                ModelState.AddModelError(
                    nameof(model.PostalCode),
                    model.CountryCode == "CA"
                        ? "You must provide a valid Canadian postal code in the format A0A 0A0"
                        : "You must provide a valid Zip Code in the format 12345 or 12345-6789");
            }
            else if (model.CountryCode == "CA" && postalCodeRegex.IsMatch(model.PostalCode))
            {
                model.PostalCode = model.PostalCode.ToUpperInvariant();

                model.PostalCode = model.PostalCode.Length == 6 
                    ? model.PostalCode.Insert(3, " ") 
                    : model.PostalCode.Replace('-', ' ');
            }
            else if (model.CountryCode == "US" && zipCodeRegex.IsMatch(model.PostalCode))
            {
                model.PostalCode = model.PostalCode.ToUpperInvariant().Replace(' ', '-');
            }

            if (!ModelState.IsValid)
            {
                this.AddAlert(AlertType.Error, "Some address information was invalid.");

                await SetupAddressesAndCountries(model);

                return View("ManageAddresses", model);
            }

            MemberAddress newAddress = new MemberAddress
            {
                MemberId = GetUserId(),
                City = model.City,
                CountryCode = model.CountryCode,
                StreetAddress = model.StreetAddress,
                POBoxNumber = model.POBoxNumber,
                ProvinceCode = model.ProvinceCode,
                PostalCode = model.PostalCode
            };

            db.MemberAddresses.Add(newAddress);

            try
            {
                await db.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                // Get the exception which states if a foreign key constraint was violated
                SqlException innermostException = ex.GetBaseException() as SqlException;

                bool errorWasProvinceForeignKeyConstraint = false;

                if (innermostException != null)
                {
                    string exMessage = innermostException.Message;
                    
                    errorWasProvinceForeignKeyConstraint =
                        innermostException.Number == (int)SqlErrorNumbers.ConstraintViolation &&
                        exMessage.Contains(nameof(Province.ProvinceCode)) &&
                        exMessage.Contains(nameof(Province.CountryCode));
                }

                this.AddAlert(AlertType.Error,
                    errorWasProvinceForeignKeyConstraint
                        ? "The Province/State you selected isn't in the Country you selected."
                        : "An unknown error occured while adding the address.");

                await SetupAddressesAndCountries(model);
        
                return View("ManageAddresses", model);
            }

            this.AddAlert(AlertType.Success, "Successfully add a new address.");
            return RedirectToAction("ManageAddresses");
        }

        public async Task<ActionResult> ManageCreditCards()
        {
            ManageCreditCardViewModel model = new ManageCreditCardViewModel();

            await SetupCreditCardsAndCountries(model);

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = VeilRoles.MEMBER_ROLE)]
        public async Task<ActionResult> CreateCreditCard(string stripeCardToken)
        {
            if (!ModelState.IsValid)
            {
                this.AddAlert(AlertType.Error, "Some credit card information is invalid.");

                return RedirectToAction("ManageCreditCards");
            }

            Member currentMember = await db.Members.FindAsync(GetUserId());

            if (currentMember == null)
            {
                // Note: There should be no way for this to happen.
                return new HttpStatusCodeResult(HttpStatusCode.InternalServerError);
            }

            MemberCreditCard newCard;

            try
            {
                newCard = stripeService.CreateCreditCard(currentMember, stripeCardToken);
            }
            catch (StripeException ex)
            {
                // Note: Stripe says their card_error messages are safe to display to the user
                if (ex.StripeError.Code == "card_error")
                {
                    this.AddAlert(AlertType.Error, ex.Message);
                    ModelState.AddModelError(STRIPE_ISSUES_MODELSTATE_KEY, ex.Message);
                }
                else
                {
                    this.AddAlert(AlertType.Error, "An error occured while talking to one of our backends. Sorry!");
                }

                return RedirectToAction("ManageCreditCards");
            }

            currentMember.CreditCards.Add(newCard);

            await db.SaveChangesAsync();

            this.AddAlert(AlertType.Success, "Successfully added a new Credit Card.");

            return RedirectToAction("ManageCreditCards");
        }

        //
        // POST: /Manage/LinkLogin
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult LinkLogin(string provider)
        {
            // Request a redirect to the external login provider to link a login for the current user
            return new AccountController.ChallengeResult(provider, Url.Action("LinkLoginCallback", "Manage"), GetUserId().ToString());
        }

        //
        // GET: /Manage/LinkLoginCallback
        public async Task<ActionResult> LinkLoginCallback()
        {
            var loginInfo = await signInManager.AuthenticationManager.GetExternalLoginInfoAsync(XsrfKey, GetUserId().ToString());
            if (loginInfo == null)
            {
                return RedirectToAction("ManageLogins", new { Message = ManageMessageId.Error });
            }
            var result = await userManager.AddLoginAsync(GetUserId(), loginInfo.Login);
            return result.Succeeded ? RedirectToAction("ManageLogins") : RedirectToAction("ManageLogins", new { Message = ManageMessageId.Error });
        }

        //
        // POST: /Manage/RememberBrowser
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult RememberBrowser()
        {
            var rememberBrowserIdentity = signInManager.AuthenticationManager.CreateTwoFactorRememberBrowserIdentity(GetUserId().ToString());
            signInManager.AuthenticationManager.SignIn(new AuthenticationProperties { IsPersistent = true }, rememberBrowserIdentity);

            return RedirectToAction("Index", "Manage");
        }

        //
        // POST: /Manage/ForgetBrowser
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ForgetBrowser()
        {
            signInManager.AuthenticationManager.SignOut(DefaultAuthenticationTypes.TwoFactorRememberBrowserCookie);

            return RedirectToAction("Index", "Manage");
        }

        #region Helpers
        // Used for XSRF protection when adding external logins
        private const string XsrfKey = "XsrfId";

        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error);
            }
        }

        private Guid GetUserId()
        {
            return idGetter.GetUserId(User.Identity);
        }

        /// <summary>
        ///     Sets up the Addresses and Countries properties of the passed <see cref="ManageAddressViewModel"/>
        /// </summary>
        /// <param name="model">
        ///     The model to setup
        /// </param>
        /// <returns>
        ///     A task to await
        /// </returns>
        private async Task SetupAddressesAndCountries(ManageAddressViewModel model)
        {
            Guid memberId = GetUserId();

            var memberAddresses = await db.MemberAddresses.
                Where(ma => ma.MemberId == memberId).
                Select(ma => 
                    new
                    {
                        ma.Id,
                        ma.StreetAddress
                    }).
                ToListAsync();

            model.Countries = await db.Countries.Include(c => c.Provinces).ToListAsync();
            model.Addresses = new SelectList(memberAddresses, nameof(MemberAddress.Id), nameof(MemberAddress.StreetAddress));
        }

        /// <summary>
        ///     Sets up the CreditCards and Countries properties of the passed <see cref="ManageCreditCardViewModel"/>
        /// </summary>
        /// <param name="model">
        ///     The model to setup
        /// </param>
        /// <returns>
        ///     A task to await
        /// </returns>
        private async Task SetupCreditCardsAndCountries(ManageCreditCardViewModel model)
        {
            const int CREDIT_CARD_NUMBER_LENGTH = 12;

            Guid memberId = GetUserId();

            var memberCreditCards =
                await db.Members.
                    Where(m => m.UserId == memberId).
                    SelectMany(m => m.CreditCards).
                    Select(cc =>
                        new
                        {
                            cc.Id,
                            cc.Last4Digits
                        }).
                    ToListAsync();

            memberCreditCards = memberCreditCards.
                Select(cc =>
                    new
                    {
                        cc.Id,
                        Last4Digits = cc.Last4Digits.PadLeft(CREDIT_CARD_NUMBER_LENGTH, '*')
                    }).
                ToList();

            model.Countries = await db.Countries.Include(c => c.Provinces).ToListAsync();
            model.CreditCards = new SelectList(memberCreditCards, nameof(MemberCreditCard.Id), nameof(MemberCreditCard.Last4Digits));
        }

        private bool HasPassword()
        {
            var user = userManager.FindById(idGetter.GetUserId(User.Identity));
            if (user != null)
            {
                return user.PasswordHash != null;
            }
            return false;
        }

        private bool HasPhoneNumber()
        {
            var user = userManager.FindById(idGetter.GetUserId(User.Identity));
            if (user != null)
            {
                return user.PhoneNumber != null;
            }
            return false;
        }

        public enum ManageMessageId
        {
            AddPhoneSuccess,
            ChangePasswordSuccess,
            SetTwoFactorSuccess,
            SetPasswordSuccess,
            RemoveLoginSuccess,
            RemovePhoneSuccess,
            Error
        }
        #endregion
    }
}