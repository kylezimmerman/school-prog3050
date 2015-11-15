/* CheckoutController.cs
 * Purpose: Controller for processing order checkout
 * 
 * Revision History:
 *      Drew Matheson, 2015.10.13: Created
 */ 

using System;
using System.Collections.Generic;
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

            WebOrderCheckoutDetails orderCheckoutDetails = Session[OrderCheckoutDetailsKey] as WebOrderCheckoutDetails;

            if (orderCheckoutDetails?.Address != null)
            {
                viewModel.StreetAddress = orderCheckoutDetails.Address.StreetAddress;
                viewModel.PostalCode = orderCheckoutDetails.Address.PostalCode;
                viewModel.POBoxNumber = orderCheckoutDetails.Address.POBoxNumber;
                viewModel.City = orderCheckoutDetails.Address.City;
                viewModel.ProvinceCode = orderCheckoutDetails.ProvinceCode;
                viewModel.CountryCode = orderCheckoutDetails.CountryCode;
            }

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> NewShippingInfo(AddressViewModel model, bool saveAddress, bool returnToConfirm = false)
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

            if (returnToConfirm)
            {
                return RedirectToAction("ConfirmOrder");
            }

            return RedirectToAction("BillingInfo");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ExistingShippingInfo(Guid addressId, bool returnToConfirm = false)
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

            if (returnToConfirm)
            {
                return RedirectToAction("ConfirmOrder");
            }

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

        public async Task<ActionResult> ConfirmOrder()
        {
            WebOrderCheckoutDetails orderCheckoutDetails =
                Session[OrderCheckoutDetailsKey] as WebOrderCheckoutDetails;

            ActionResult invalidSessionResult = EnsureValidSessionForConfirmStep(orderCheckoutDetails);

            if (invalidSessionResult != null)
            {
                return invalidSessionResult;
            }

            Contract.Assume(orderCheckoutDetails != null);

            Guid memberId = GetUserId();

            var memberInfo =
                await db.Users.
                    Where(m => m.Id == memberId).
                    Select(u => 
                        new
                        {
                            FullName = u.FirstName + " " + u.LastName,
                            PhoneNumber = u.PhoneNumber
                        }
                    ).
                    SingleOrDefaultAsync();

            /* Setup the cart / cart item information */
            Cart cart =
                await db.Members.
                    Where(m => m.UserId == memberId).
                    Select(m => m.Cart).
                    Include(c => c.Items).
                    Include(c => c.Items.Select(ci => ci.Product)).
                    SingleOrDefaultAsync();

            List<ConfirmOrderCartItem> cartItems = 
                cart.Items.
                    Select(ci => 
                        new ConfirmOrderCartItem
                        {
                            IsNew = ci.IsNew ? "Yes" : "No",
                            ItemPrice = ci.IsNew ? ci.Product.NewWebPrice : ci.Product.UsedWebPrice.Value,
                            Name = ci.Product.Name,
                            PlatformName = ci.Product is PhysicalGameProduct ? ((PhysicalGameProduct)ci.Product).Platform.PlatformName : "",
                            Quantity = ci.Quantity
                        }
                    ).ToList();

            /* Setup the address information */
            Address address;
            string provinceCode;
            string countryCode;

            if (orderCheckoutDetails.MemberAddressId != null)
            {
                MemberAddress memberAddress =
                    await db.MemberAddresses.FindAsync(orderCheckoutDetails.MemberAddressId);

                if (memberAddress == null)
                {
                    this.AddAlert(AlertType.Error, "The shipping address you selected could not be found.");

                    return RedirectToAction("ShippingInfo");
                }

                address = memberAddress.Address;
                provinceCode = memberAddress.ProvinceCode;
                countryCode = memberAddress.CountryCode;
            }
            else
            {
                address = orderCheckoutDetails.Address;
                provinceCode = orderCheckoutDetails.ProvinceCode;
                countryCode = orderCheckoutDetails.CountryCode;
            }

            Province province = await db.Provinces.FindAsync(provinceCode, countryCode);
            Country country = await db.Countries.FindAsync(countryCode);

            /* Setup the credit card information */
            string last4Digits;

            if (orderCheckoutDetails.MemberCreditCardId != null)
            {
                last4Digits = await db.Members.
                    Where(m => m.UserId == memberId).
                    SelectMany(m => m.CreditCards).
                    Where(cc => cc.Id == orderCheckoutDetails.MemberAddressId.Value).
                    Select(cc => cc.Last4Digits).
                    SingleOrDefaultAsync();

                if (last4Digits == null)
                {
                    this.AddAlert(AlertType.Error, "The billing information you selected could not be found.");

                    return RedirectToAction("BillingInfo");
                }
            }
            else
            {
                last4Digits = stripeService.GetLast4ForToken(orderCheckoutDetails.StripeCardToken);
            }

            last4Digits = last4Digits.PadLeft(16, '*').Insert(4, " ").Insert(9, " ").Insert(14, " ");

            /* Setup the view model with the gathered information */
            ConfirmOrderViewModel webOrder = new ConfirmOrderViewModel
            {
                FullName = memberInfo.FullName,
                PhoneNumber = memberInfo.PhoneNumber,
                Address = address,
                ProvinceName = province.Name,
                CountryName = country.CountryName,
                CreditCardLast4Digits = last4Digits,
                Items = cartItems,
                ItemSubTotal = cartItems.Sum(ci => ci.ItemTotal)
            };

            webOrder.ShippingCost = webOrder.ItemSubTotal < 120m ? 12.00m : 0m;
            webOrder.TaxAmount = webOrder.ItemSubTotal * (province.ProvincialTaxRate + country.FederalTaxRate);

            return View(webOrder);
        }

        // POST: Checkout/PlaceOrder
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> PlaceOrder()
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
                    "You must provide your shipping information before providing billing information.");

                return RedirectToAction("ShippingInfo");
            }

            return null;
        }

        private ActionResult EnsureValidSessionForConfirmStep(WebOrderCheckoutDetails checkoutDetails)
        {
            if (checkoutDetails == null ||
                (checkoutDetails.MemberAddressId == null && checkoutDetails.Address == null))
            {
                this.AddAlert(AlertType.Info,
                    "You must provide your shipping information before confirming your order.");

                return RedirectToAction("ShippingInfo");
            }

            if (checkoutDetails.StripeCardToken == null && checkoutDetails.MemberCreditCardId == null)
            {
                this.AddAlert(AlertType.Info,
                    "You must provide your billing information before confirming your order.");

                return RedirectToAction("BillingInfo");
            }

            return null;
        }
    }
}