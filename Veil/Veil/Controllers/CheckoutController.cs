/* CheckoutController.cs
 * Purpose: Controller for processing order checkout
 * 
 * Revision History:
 *      Drew Matheson, 2015.10.13: Created
 */ 

using System;
using System.Data.Entity;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web.Mvc;
using Stripe;
using Veil.DataAccess.Interfaces;
using Veil.DataModels.Models;
using Veil.Helpers;
using Veil.Models;
using Veil.Services.Interfaces;

namespace Veil.Controllers
{
    public class CheckoutController : BaseController
    {
        private static string OrderCheckoutDetailsKey = "CheckoutController.OrderCheckoutDetails";

        private readonly IVeilDataAccess db;
        private readonly IGuidUserIdGetter idGetter;
        private readonly IStripeService stripeService;

        public CheckoutController(IVeilDataAccess veilDataAccess, IGuidUserIdGetter idGetter, IStripeService stripeService)
        {
            db = veilDataAccess;
            this.idGetter = idGetter;
            this.stripeService = stripeService;
        }

        [HttpGet]
        public async Task<ActionResult> ShippingInfo()
        {
            AddressViewModel viewModel = new AddressViewModel();

            await viewModel.SetupAddressesAndCountries(db, GetUserId());

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> NewShippingInfo(AddressViewModel model, bool saveAddress)
        {
            if (!ModelState.IsValid)
            {
                model.UpdatePostalCodeModelError(ModelState);

                this.AddAlert(AlertType.Error, "Some address information was invalid.");

                await model.SetupAddressesAndCountries(db, GetUserId());

                return View("ShippingInfo", model);
            }

            bool validCountry = await db.Countries.AnyAsync(c => c.CountryCode == model.CountryCode);

            if (!validCountry)
            {
                this.AddAlert(AlertType.Error, "The Country you selected isn't valid.");
            }

            bool validProvince =
                await db.Provinces.AnyAsync(
                        p => p.CountryCode == model.CountryCode &&
                        p.ProvinceCode == model.ProvinceCode);

            if (!validProvince)
            {
                this.AddAlert(
                    AlertType.Error, "The Province/State you selected isn't in the Country you selected.");
            }

            if (!validCountry || !validProvince)
            {
                await model.SetupAddressesAndCountries(db, GetUserId());

                return View("ShippingInfo", model);
            }

            WebOrderCheckoutDetails orderCheckoutDetails = 
                Session[OrderCheckoutDetailsKey] as WebOrderCheckoutDetails ?? new WebOrderCheckoutDetails();

            model.FormatPostalCode();

            if (saveAddress)
            {
                MemberAddress newAddress = new MemberAddress
                {
                    MemberId = GetUserId(),
                    Address = model.MapToNewAddress(),
                    CountryCode = model.CountryCode,
                    ProvinceCode = model.ProvinceCode
                };

                db.MemberAddresses.Add(newAddress);

                await db.SaveChangesAsync();

                this.AddAlert(AlertType.Success, "Successfully add the new address.");

                orderCheckoutDetails.MemberAddressId = newAddress.Id;
            }
            else
            {
                orderCheckoutDetails.Address = model.MapToNewAddress();
                orderCheckoutDetails.CountryCode = model.CountryCode;
                orderCheckoutDetails.ProvinceCode = model.ProvinceCode;
            }

            Session[OrderCheckoutDetailsKey] = orderCheckoutDetails;

            return RedirectToAction("BillingInfo");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ExistingShippingInfo(Guid addressId)
        {
            if (!await db.MemberAddresses.AnyAsync(ma => ma.Id == addressId))
            {
                AddressViewModel model = new AddressViewModel();
                await model.SetupAddressesAndCountries(db, GetUserId());

                this.AddAlert(AlertType.Error, "The address you selected could not be found.");

                return View("ShippingInfo", model);
            }

            WebOrderCheckoutDetails orderCheckoutDetails =
                Session[OrderCheckoutDetailsKey] as WebOrderCheckoutDetails ?? new WebOrderCheckoutDetails();

            orderCheckoutDetails.MemberAddressId = addressId;
            
            Session[OrderCheckoutDetailsKey] = orderCheckoutDetails;

            return RedirectToAction("BillingInfo");
        }

        // GET: Checkout/BillingInfo
        [HttpGet]
        public async Task<ActionResult> BillingInfo()
        {
            WebOrderCheckoutDetails orderCheckoutDetails =
                Session[OrderCheckoutDetailsKey] as WebOrderCheckoutDetails;

            ActionResult invalidSessionResult = EnsureValidSessionForBillingStep(orderCheckoutDetails);

            if (invalidSessionResult != null)
            {
                return invalidSessionResult;
            }

            BillingInfoViewModel viewModel = new BillingInfoViewModel();

            await viewModel.SetupCreditCardsAndCountries(db, GetUserId());

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> NewBillingInfo(string stripeCardToken, bool saveCard)
        {
            WebOrderCheckoutDetails orderCheckoutDetails =
                Session[OrderCheckoutDetailsKey] as WebOrderCheckoutDetails;

            ActionResult invalidSessionResult = EnsureValidSessionForBillingStep(orderCheckoutDetails);

            if (invalidSessionResult != null)
            {
                return invalidSessionResult;
            }

            Contract.Assume(orderCheckoutDetails != null);

            if (saveCard)
            {
                if (string.IsNullOrWhiteSpace(stripeCardToken))
                {
                    this.AddAlert(AlertType.Error, "Some credit card information is invalid.");

                    return RedirectToAction("BillingInfo");
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
                        ModelState.AddModelError(ManageController.STRIPE_ISSUES_MODELSTATE_KEY, ex.Message);
                    }
                    else
                    {
                        this.AddAlert(AlertType.Error, "An error occured while talking to one of our backends. Sorry!");
                    }

                    return RedirectToAction("BillingInfo");
                }

                currentMember.CreditCards.Add(newCard);

                await db.SaveChangesAsync();

                this.AddAlert(AlertType.Success, "Successfully added the new Credit Card.");

                orderCheckoutDetails.MemberCreditCardId = newCard.Id;
            }
            else
            {
                orderCheckoutDetails.StripeCardToken = stripeCardToken;
            }

            Session[OrderCheckoutDetailsKey] = orderCheckoutDetails;

            return RedirectToAction("ConfirmOrder");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ExistingBillingInfo(Guid cardId)
        {
            WebOrderCheckoutDetails orderCheckoutDetails =
                Session[OrderCheckoutDetailsKey] as WebOrderCheckoutDetails;

            ActionResult invalidSessionResult = EnsureValidSessionForBillingStep(orderCheckoutDetails);

            if (invalidSessionResult != null)
            {
                return invalidSessionResult;
            }

            Contract.Assume(orderCheckoutDetails != null);

            Guid memberId = GetUserId();

            if (!await db.Members.Where(m => m.UserId == memberId).AnyAsync(m => m.CreditCards.Any(cc => cc.Id == cardId)))
            {
                BillingInfoViewModel model = new BillingInfoViewModel();
                await model.SetupCreditCardsAndCountries(db, memberId);

                this.AddAlert(AlertType.Error, "The card you selected could not be found.");

                return View("BillingInfo", model);
            }

            orderCheckoutDetails.MemberCreditCardId = cardId;

            Session[OrderCheckoutDetailsKey] = orderCheckoutDetails;

            return RedirectToAction("ConfirmOrder");
        }

        // GET: Checkout/ConfirmOrder
        public async Task<ActionResult> ConfirmOrder()
        {
            // TODO: Display summary (item cost, shipping cost, total before tax, total with tax)
            // TODO: Display items in the order
            // TODO: Display shipping info
            // TODO: Display payment info

            WebOrderCheckoutDetails orderCheckoutDetails =
                Session[OrderCheckoutDetailsKey] as WebOrderCheckoutDetails;

            ActionResult invalidSessionResult = EnsureValidSessionForBillingStep(orderCheckoutDetails);

            if (invalidSessionResult != null)
            {
                return invalidSessionResult;
            }

            WebOrder order = new WebOrder();
            Member currentMember = new Member();

            //CheckoutViewModel webOrder = new CheckoutViewModel()
            //{
            //    Address = order.ShippingAddress,
            //    BillingInfo = order.MemberCreditCard,
            //    Items = currentMember.Cart.Items
            //};

            //return View(webOrder);
            return View();
        }

        // POST: Checkout/CompleteOrder
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> CompleteOrder()
        {
            // TODO: Persist order
            // TODO: Convert cart items to web order item

            Session.Remove(OrderCheckoutDetailsKey);

            return RedirectToAction("Index", "Home");
        }

        private Guid GetUserId()
        {
            return idGetter.GetUserId(User.Identity);
        }

        private ActionResult EnsureValidSessionForBillingStep(WebOrderCheckoutDetails checkoutDetails)
        {
            if (checkoutDetails == null || 
                (checkoutDetails.MemberAddressId == null && checkoutDetails.Address == null))
            {
                this.AddAlert(AlertType.Info,
                    "You must select your shipping information before choosing billing information.");

                return RedirectToAction("ShippingInfo");
            }

            Contract.Assert(checkoutDetails != null);

            return null;
        }
    }
}