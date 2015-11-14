/* CheckoutController.cs
 * Purpose: Controller for processing order checkout
 * 
 * Revision History:
 *      Drew Matheson, 2015.10.13: Created
 */ 

using System;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using Veil.DataAccess.Interfaces;
using Veil.DataModels.Models;
using Veil.Helpers;
using Veil.Models;

namespace Veil.Controllers
{
    public class CheckoutController : BaseController
    {
        private static string OrderCheckoutDetailsKey = "CheckoutController.OrderCheckoutDetails";

        private readonly IVeilDataAccess db;
        private readonly IGuidUserIdGetter idGetter;

        public CheckoutController(IVeilDataAccess veilDataAccess, IGuidUserIdGetter idGetter)
        {
            db = veilDataAccess;
            this.idGetter = idGetter;
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
            WebOrderCheckoutDetails incompleteOrder = Session[OrderCheckoutDetailsKey] as WebOrderCheckoutDetails;

            if (incompleteOrder == null)
            {
                // TODO: Add Alert
                return RedirectToAction("ShippingInfo");
            }

            BillingInfoViewModel viewModel = new BillingInfoViewModel();

            await viewModel.SetupCreditCardsAndCountries(db, GetUserId());

            return View(viewModel);
        }

        // POST: Checkout/BillingInfo
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> BillingInfo(MemberCreditCard billingInfo)
        {
            // TODO: Add billing info to the session WebOrder

            return RedirectToAction("ConfirmOrder");
        }

        // GET: Checkout/ConfirmOrder
        public async Task<ActionResult> ConfirmOrder()
        {
            // TODO: Display summary (item cost, shipping cost, total before tax, total with tax)
            // TODO: Display items in the order
            // TODO: Display shipping info
            // TODO: Display payment info

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
    }
}