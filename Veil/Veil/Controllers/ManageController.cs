/* ManageController.cs
 * Purpose: Controller for manage account related things
 * 
 * Revision History:
 *      Drew Matheson, 2015.09.25: Created
 */ 

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.Validation;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Veil.DataAccess.Interfaces;
using Veil.DataModels;
using Veil.DataModels.Models;
using Veil.DataModels.Models.Identity;
using Veil.Exceptions;
using Veil.Helpers;
using Veil.Models;
using Veil.Services;
using Veil.Services.Interfaces;
using Member = Veil.DataModels.Models.Member;

namespace Veil.Controllers
{
    /// <summary>
    ///     Controller for manage account related things.
    ///     The actions in this controller can only be viewed by users in the Member role
    /// </summary>
    [Authorize(Roles = VeilRoles.MEMBER_ROLE)]
    public class ManageController : BaseController
    {
        public const string STRIPE_ISSUES_MODELSTATE_KEY = "StripeIssues";

        private readonly VeilSignInManager signInManager;
        private readonly VeilUserManager userManager;
        private readonly IVeilDataAccess db;
        private readonly IGuidUserIdGetter idGetter;
        private readonly IStripeService stripeService;

        /// <summary>
        ///     Instantiates a new instance of ManageController with the provided arguments
        /// </summary>
        /// <param name="userManager">
        ///     The <see cref="VeilUserManager"/> for the controller to use
        /// </param>
        /// <param name="signInManager">
        ///     The <see cref="VeilSignInManager"/> for the controller to use
        /// </param>
        /// <param name="veilDataAccess">
        ///     The <see cref="IVeilDataAccess"/> to use for database access
        /// </param>
        /// <param name="idGetter">
        ///     The <see cref="IGuidUserIdGetter"/> to use for getting the current user's Id
        /// </param>
        /// <param name="stripeService">
        ///     The <see cref="IStripeService"/> to use for Stripe interaction
        /// </param>
        public ManageController(
            VeilUserManager userManager, VeilSignInManager signInManager, IVeilDataAccess veilDataAccess,
            IGuidUserIdGetter idGetter, IStripeService stripeService)
        {
            this.userManager = userManager;
            this.signInManager = signInManager;
            db = veilDataAccess;
            this.idGetter = idGetter;
            this.stripeService = stripeService;
        }

        /// <summary>
        ///     Displays a view allowing the member to manage their account, including 
        ///     addresses, passwords and favorite tags and platforms
        /// </summary>
        /// <param name="message">
        ///     Enum value for a message to display with the page
        /// </param>
        /// <returns>
        ///     The view allowing the member to manage their account
        /// </returns>
        public async Task<ActionResult> Index(ManageMessageId? message)
        {
            switch (message)
            {
                case ManageMessageId.AddPhoneSuccess:
                    this.AddAlert(AlertType.Success, "Your phone number was added.");
                    break;
                case ManageMessageId.ChangePasswordSuccess:
                    this.AddAlert(AlertType.Success, "Your password has been changed.");
                    break;
                case ManageMessageId.SetTwoFactorSuccess:
                    this.AddAlert(
                        AlertType.Success, "Your two-factor authentication provider has been set.");
                    break;
                case ManageMessageId.SetPasswordSuccess:
                    this.AddAlert(AlertType.Success, "Your password has been set.");
                    break;
                case ManageMessageId.RemoveLoginSuccess:
                    this.AddAlert(AlertType.Success, "A login has been removed.");
                    break;
                case ManageMessageId.RemovePhoneSuccess:
                    this.AddAlert(AlertType.Success, "Your phone number was removed.");
                    break;
                case ManageMessageId.Error:
                    this.AddAlert(AlertType.Error, "An error has occurred.");
                    break;
            }

            var userId = GetUserId();
            var user = await userManager.FindByIdAsync(userId);

            if (user == null)
            {
                // If this happens, the user has been deleted in the database but still has a valid login cookie
                signInManager.AuthenticationManager.SignOut(DefaultAuthenticationTypes.ApplicationCookie);
                return RedirectToAction("Index", "Home");
            }

            if (user.Member == null)
            {
                this.AddAlert(AlertType.Error, "Employees do not have profiles to view.");
                return RedirectToAction("Index", "Home");
            }

            var model = new IndexViewModel
            {
                PhoneNumber = user.PhoneNumber,
                MemberFirstName = user.FirstName,
                MemberLastName = user.LastName,
                MemberEmail = user.Email,
                MemberVisibility = user.Member.WishListVisibility,
                ReceivePromotionalEmails = user.Member.ReceivePromotionalEmails,
                FavoritePlatformCount = user.Member.FavoritePlatforms.Count,
                FavoriteTagCount = user.Member.FavoriteTags.Count
            };

            return View(model);
        }

        /// <summary>
        ///     Updates the user's profile information
        /// </summary>
        /// <param name="viewModel">
        ///     <see cref="IndexViewModel"/> containing the updated profile information
        /// </param>
        /// <returns>
        ///     Redirection back to Index if successful.
        ///     Redirection to Home Index if the user doesn't exist
        ///     Redirection to Home Index if the user is an employee
        ///     Redisplays the form if the information is incorrect
        /// </returns>
        [HttpPost]
        [ActionName(nameof(Index))]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> UpdateProfile(IndexViewModel viewModel)
        {
            Guid userId = GetUserId();
            var user = await userManager.FindByIdAsync(userId);
            bool isNewEmail = false;

            if (user == null)
            {
                // If this happens, the user has been deleted in the database but still has a valid login cookie
                signInManager.AuthenticationManager.SignOut(DefaultAuthenticationTypes.ApplicationCookie);
                return RedirectToAction("Index", "Home");
            }

            if (user.Member == null)
            {
                this.AddAlert(AlertType.Error, "Employees do not have public profiles.");
                return RedirectToAction("Index", "Home");
            }

            if (ModelState.IsValid)
            {
                user.FirstName = viewModel.MemberFirstName;
                user.LastName = viewModel.MemberLastName;
                user.PhoneNumber = viewModel.PhoneNumber;
                user.Member.ReceivePromotionalEmails = viewModel.ReceivePromotionalEmails;
                user.Member.WishListVisibility = viewModel.MemberVisibility;

                if (user.Email != viewModel.MemberEmail)
                {
                    // Runs if newEmail was not null or empty and and the new email property is 
                    // different than the one being set
                    if (!string.IsNullOrWhiteSpace(user.NewEmail) &&
                        user.NewEmail != viewModel.MemberEmail)
                    {
                        // Invalidates confirmation email stamp
                        await userManager.UpdateSecurityStampAsync(userId);
                    }
                    user.NewEmail = viewModel.MemberEmail;
                    isNewEmail = true;
                }

                try
                {
                    db.MarkAsModified(user);
                    await db.SaveChangesAsync();
                    if (isNewEmail)
                    {
                        await SendConfirmationEmail(user);
                        this.AddAlert(
                            AlertType.Info, "A confirmation email has been sent to " + user.NewEmail +
                                ". You must continue logging into your Veil account using " +
                                user.Email + " until you confirm the new email address");
                    }
                    this.AddAlert(AlertType.Success, "Your Profile has been updated");
                }
                catch (DataException)
                {
                    this.AddAlert(AlertType.Error, "An error occurred while trying to save your changes");
                }
            }
            else
            {
                this.AddAlert(AlertType.Error, "Some profile information was invalid");

                viewModel.FavoritePlatformCount = user.Member.FavoritePlatforms.Count;
                viewModel.FavoriteTagCount = user.Member.FavoriteTags.Count;

                return View(viewModel);
            }

            return RedirectToAction("Index");
        }

        /// <summary>
        ///     Validates the confirmation info, sets the user's email as confirmed, 
        ///     and sets the new email address as their email address
        /// </summary>
        /// <param name="userId">
        ///     The Id of the user
        /// </param>
        /// <param name="code">
        ///     The validation code
        /// </param>
        /// <returns>
        ///     If successful, a view letting the user know their email has confirmed.
        ///     Otherwise, an error view.
        /// </returns>
        public async Task<ActionResult> ConfirmNewEmail(Guid userId, string code)
        {
            if (userId == Guid.Empty || string.IsNullOrWhiteSpace(code))
            {
                return View("Error");
            }

            var result = await userManager.ConfirmEmailAsync(userId, code);

            if (!result.Succeeded)
            {
                AddErrors(result);

                return View("Error");
            }

            var user = await userManager.FindByIdAsync(userId);

            if (user == null)
            {
                throw new InvalidOperationException();
            }

            user.Email = user.NewEmail;
            user.NewEmail = null;

            try
            {
                db.MarkAsModified(user);
                await db.SaveChangesAsync();

                // Update the security stamp to invalidate the email link
                await userManager.UpdateSecurityStampAsync(userId);
            }
            catch (DataException)
            {
                this.AddAlert(
                    AlertType.Error,
                    "There was an error confirming new email email address please try again later");
                return View("Error");
            }

            return View("ConfirmNewEmail");
        }

        /// <summary>
        ///     Displays a view for changed your account password
        /// </summary>
        /// <returns></returns>
        public ViewResult ChangePassword()
        {
            return View();
        }

        /// <summary>
        ///     Changes the user's password
        /// </summary>
        /// <param name="model">
        ///     <see cref="ChangePasswordViewModel"/> containing the old, new, and confirm passwords
        /// </param>
        /// <returns>
        ///     Redirects to Index if successful
        ///     Redisplays the form if information is invalid
        ///     Redisplays the form if password changing fails due to a database error
        ///     Redisplays the form if password changing fails due to an Identity error
        /// </returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            IdentityResult result;

            try
            {
                result = await userManager.
                    ChangePasswordAsync(GetUserId(), model.OldPassword, model.NewPassword);
            }
            catch (DbEntityValidationException ex)
            {
                this.AddAlert(AlertType.Error, ex.Message);

                return View(model);
            }

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

        /// <summary>
        ///     Displays a view for adding or removing addresses
        /// </summary>
        /// <returns>
        ///     The view.
        /// </returns>
        public async Task<ActionResult> ManageAddresses()
        {
            AddressViewModel model = new AddressViewModel();

            await model.SetupAddressesAndCountries(db, GetUserId());

            return View(model);
        }

        /// <summary>
        ///     Creates a new address for the member
        /// </summary>
        /// <param name="model">
        ///     <see cref="AddressViewModel"/> containing the address details
        /// </param>
        /// <returns>
        ///     Redirects back to ManageAddresses if successful
        ///     Redisplays the form if the information is invalid or a database error occurs
        /// </returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> CreateAddress(AddressViewModel model)
        {
            model.UpdatePostalCodeModelError(ModelState);

            if (!ModelState.IsValid)
            {
                this.AddAlert(AlertType.Error, "Some address information was invalid.");

                await model.SetupAddressesAndCountries(db, GetUserId());

                return View("ManageAddresses", model);
            }

            model.FormatPostalCode();

            MemberAddress newAddress = new MemberAddress
            {
                MemberId = GetUserId(),
                Address = model.MapToNewAddress(),
                ProvinceCode = model.ProvinceCode,
                CountryCode = model.CountryCode
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
                        innermostException.Number == (int) SqlErrorNumbers.ConstraintViolation &&
                            exMessage.Contains(nameof(Province.ProvinceCode)) &&
                            exMessage.Contains(nameof(Province.CountryCode));
                }

                this.AddAlert(
                    AlertType.Error,
                    errorWasProvinceForeignKeyConstraint
                        ? "The Province/State you selected isn't in the Country you selected."
                        : "An unknown error occured while adding the address.");

                await model.SetupAddressesAndCountries(db, GetUserId());

                return View("ManageAddresses", model);
            }

            this.AddAlert(AlertType.Success, "Successfully add a new address.");
            return RedirectToAction("ManageAddresses");
        }

        /// <summary>
        ///     Allows the member to edit an existing <see cref="MemberAddress"/>
        /// </summary>
        /// <param name="addressId">
        ///     The Id of the <see cref="MemberAddress"/> to be edited
        /// </param>
        /// <returns>
        ///     A view allowing the member to edit the address
        /// </returns>
        [HttpGet]
        public async Task<ActionResult> EditAddress(Guid? addressId)
        {
            if (addressId == null)
            {
                throw new HttpException(NotFound, "Address");
            }

            MemberAddress addressToEdit = await db.MemberAddresses.FindAsync(addressId);

            if (addressToEdit == null)
            {
                throw new HttpException(NotFound, "Address");
            }

            AddressViewModel model = new AddressViewModel
            {
                Id = addressToEdit.Id,
                StreetAddress = addressToEdit.Address.StreetAddress,
                POBoxNumber = addressToEdit.Address.POBoxNumber,
                City = addressToEdit.Address.City,
                PostalCode = addressToEdit.Address.PostalCode,
                ProvinceCode = addressToEdit.ProvinceCode,
                CountryCode = addressToEdit.CountryCode
            };

            await model.SetupCountries(db);

            return View(model);
        }

        /// <summary>
        ///     Creates a new address for the member
        /// </summary>
        /// <param name="id">
        ///     The Id of the address to edit
        /// </param>
        /// <param name="model">
        ///     <see cref="AddressViewModel"/> containing the address details
        /// </param>
        /// <returns>
        ///     Redirects back to ManageAddresses if successful
        ///     Redisplays the form if the information is invalid or a database error occurs
        /// </returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> EditAddress(Guid id, AddressViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.UpdatePostalCodeModelError(ModelState);

                this.AddAlert(AlertType.Error, "Some address information was invalid.");

                await model.SetupCountries(db);

                return View("ManageAddresses", model);
            }

            MemberAddress editedAddress = await db.MemberAddresses.FindAsync(id);

            if (editedAddress == null)
            {
                throw new HttpException(NotFound, "Address");
            }

            model.FormatPostalCode();

            editedAddress.Address = model.MapToNewAddress();
            editedAddress.ProvinceCode = model.ProvinceCode;
            editedAddress.CountryCode = model.CountryCode;

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
                        innermostException.Number == (int) SqlErrorNumbers.ConstraintViolation &&
                            exMessage.Contains(nameof(Province.ProvinceCode)) &&
                            exMessage.Contains(nameof(Province.CountryCode));
                }

                this.AddAlert(
                    AlertType.Error,
                    errorWasProvinceForeignKeyConstraint
                        ? "The Province/State you selected isn't in the Country you selected."
                        : "An unknown error occured while adding the address.");

                await model.SetupCountries(db);

                return View(model);
            }

            this.AddAlert(AlertType.Success, "Successfully updated the address.");
            return RedirectToAction("ManageAddresses");
        }

        /// <summary>
        ///     Deletes the specified address
        /// </summary>
        /// <param name="id">
        ///     The Id of the address to remove
        /// </param>
        /// <returns>
        ///     Redirection to ManageAddresses if successful
        ///     404 Not Found view if the id does not match a credit card
        /// </returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteAddress(Guid id)
        {
            MemberAddress address = await db.MemberAddresses.FindAsync(id);

            if (address == null)
            {
                throw new HttpException(NotFound, nameof(MemberAddress));
            }

            db.MemberAddresses.Remove(address);
            await db.SaveChangesAsync();

            this.AddAlert(AlertType.Success, "Successfully removed the address.");

            return RedirectToAction("ManageAddresses");
        }

        /// <summary>
        ///     Displays a view allowing the member to add or remove credit cards
        /// </summary>
        /// <returns>
        ///     The view allowing the member to add or remove credit cards
        /// </returns>
        public async Task<ActionResult> ManageCreditCards()
        {
            BillingInfoViewModel model = new BillingInfoViewModel();

            await model.SetupCreditCardsAndCountries(db, GetUserId());

            return View(model);
        }

        /// <summary>
        ///     Creates a new credit card using a Stripe card token
        /// </summary>
        /// <param name="stripeCardToken">
        ///     The Stripe card token for the new credit card
        /// </param>
        /// <returns>
        ///     Redirection to ManageCreditCards if successful
        ///     Redirection to ManageCreditCards if <see cref="stripeCardToken"/> is invalid
        ///     500 Internal Server Error page if the current member doesn't exist.
        ///     Redirection to ManageCreditCards if associating the card 
        ///         with the Member's customer account fails
        /// </returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = VeilRoles.MEMBER_ROLE)]
        public async Task<ActionResult> CreateCreditCard(string stripeCardToken)
        {
            if (string.IsNullOrWhiteSpace(stripeCardToken))
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
            catch (StripeServiceException ex)
            {
                switch (ex.ExceptionType) {
                    case StripeExceptionType.CardError:
                        ModelState.AddModelError(STRIPE_ISSUES_MODELSTATE_KEY, ex.Message);
                        break;
                    case StripeExceptionType.ApiKeyError:
                        return new HttpStatusCodeResult(HttpStatusCode.InternalServerError);
                }

                this.AddAlert(AlertType.Error, ex.Message);

                return RedirectToAction("ManageCreditCards");
            }

            currentMember.CreditCards.Add(newCard);

            await db.SaveChangesAsync();

            this.AddAlert(AlertType.Success, "Successfully added a new Credit Card.");

            return RedirectToAction("ManageCreditCards");
        }

        /// <summary>
        ///     Deletes the specified credit card
        /// </summary>
        /// <param name="id">
        ///     The Id of the credit card
        /// </param>
        /// <returns>
        ///     A redirection back to ManageCreditCards if successful.
        ///     404 Not Found view if the id does not match a credit card
        /// </returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteCreditCard(Guid id)
        {
            MemberCreditCard card = await db.MemberCreditCards.FindAsync(id);

            if (card == null)
            {
                throw new HttpException(NotFound, "Credit Card");
            }

            db.MemberCreditCards.Remove(card);
            await db.SaveChangesAsync();

            this.AddAlert(AlertType.Success, "Successfully removed the credit card.");

            return RedirectToAction("ManageCreditCards");
        }

        /// <summary>
        ///     Allows a member to update their favorite platforms
        /// </summary>
        /// <returns>
        ///     A view containing a form to set favorite platforms
        /// </returns>
        public async Task<ActionResult> ManagePlatforms()
        {
            Member currentMember = await db.Members.FindAsync(idGetter.GetUserId(User.Identity));

            return View(currentMember.FavoritePlatforms.ToList());
        }

        /// <summary>
        ///     Persists the selected platforms as the member's new favorites
        /// </summary>
        /// <param name="platforms">
        ///     An updated list of the member's favorite platforms
        /// </param>
        /// <returns>
        ///     Redirects to the index view.
        /// </returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ManagePlatforms(List<string> platforms)
        {
            if (platforms == null)
            {
                platforms = new List<string>();
            }

            Member currentMember = await db.Members.FindAsync(idGetter.GetUserId(User.Identity));

            currentMember.FavoritePlatforms.Clear();
            currentMember.FavoritePlatforms =
                await db.Platforms.Where(p => platforms.Contains(p.PlatformCode)).ToListAsync();

            db.MarkAsModified(currentMember);
            await db.SaveChangesAsync();

            this.AddAlert(AlertType.Success, "Favorite platforms updated.");

            return RedirectToAction("Index");
        }

        /// <summary>
        ///     Allows a member to update their favorite tags
        /// </summary>
        /// <returns>
        ///     A view containing a form to set favorite tags
        /// </returns>
        public async Task<ActionResult> ManageTags()
        {
            Member currentMember = await db.Members.FindAsync(idGetter.GetUserId(User.Identity));

            return View(currentMember.FavoriteTags.ToList());
        }

        /// <summary>
        ///     Persists the selected tags as the member's new favorites
        /// </summary>
        /// <param name="tags">
        ///     An updated list of the member's favorite tags
        /// </param>
        /// <returns>
        ///     Redirects to the index view.
        /// </returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ManageTags(List<string> tags)
        {
            if (tags == null)
            {
                tags = new List<string>();
            }

            Member currentMember = await db.Members.FindAsync(idGetter.GetUserId(User.Identity));

            currentMember.FavoriteTags.Clear();
            currentMember.FavoriteTags = await db.Tags.Where(t => tags.Contains(t.Name)).ToListAsync();

            db.MarkAsModified(currentMember);
            await db.SaveChangesAsync();

            this.AddAlert(AlertType.Success, "Favorite tags updated.");

            return RedirectToAction("Index");
        }

        #region Helpers
        /// <summary>
        ///     Adds all errors in an IdentityResult to ModelState
        /// </summary>
        /// <param name="result">
        ///     The <see cref="IdentityResult"/> to add errors from
        /// </param>
        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error);
            }
        }

        /// <summary>
        ///     Gets the current user's Id
        /// </summary>
        /// <returns>
        ///     The current user's Id
        /// </returns>
        private Guid GetUserId()
        {
            return idGetter.GetUserId(User.Identity);
        }

        /// <summary>
        ///     Enumeration of identifiers for possible status messages
        /// </summary>
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

        /// <summary>
        ///     Sends a confirmation email allowing the user to confirm a new email address
        /// </summary>
        /// <param name="user">
        ///     The <see cref="User"/> to see the confirmation email to
        /// </param>
        /// <returns>
        ///     A Task to be awaited
        /// </returns>
        private async Task SendConfirmationEmail(User user)
        {
            string code = await userManager.GenerateEmailConfirmationTokenAsync(user.Id);
            var callbackUrl = Url.Action(
                "ConfirmNewEmail", "Manage",
                new
                {
                    userId = user.Id,
                    code = code
                },
                protocol: Request.Url.Scheme);

            await userManager.SendNewEmailConfirmationEmailAsync(
                user.NewEmail,
                "Veil - Email change request",
                "<h1>Confirm this email to use it at Veil</h1>" +
                    "An email change request has been made for this address. If you requested this please click <a href=\"" +
                    callbackUrl + "\">here</a>." +
                    "<br/> **Note once you click this link you need to use this email address to log in to Veil");
        }
        #endregion
    }
}