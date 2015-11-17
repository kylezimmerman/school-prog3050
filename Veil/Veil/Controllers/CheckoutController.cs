/* CheckoutController.cs
 * Purpose: Controller for processing order checkout
 * 
 * Revision History:
 *      Drew Matheson, 2015.10.13: Created
 */ 

using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Transactions;
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
        private readonly IShippingCostService shippingCostService;

        public CheckoutController(IVeilDataAccess veilDataAccess, IGuidUserIdGetter idGetter, IStripeService stripeService, IShippingCostService shippingCostService)
        {
            db = veilDataAccess;
            this.idGetter = idGetter;
            this.stripeService = stripeService;
            this.shippingCostService = shippingCostService;
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

            Cart cart = await GetCartWithLoadedProductsAsync(memberId);
            var cartItems = GetConfirmOrderCartItems(cart);

            /* Setup the address information */
            MemberAddress memberAddress = await GetMemberAddress(orderCheckoutDetails);

            if (memberAddress == null)
            {
                this.AddAlert(AlertType.Error, "The shipping address you selected could not be found.");

                return RedirectToAction("ShippingInfo");
            }

            /* Setup the credit card information */
            string last4Digits = await GetLast4DigitsAsync(orderCheckoutDetails, memberId);

            if (last4Digits == null)
            {
                this.AddAlert(AlertType.Error, "The billing information you selected could not be found.");

                return RedirectToAction("BillingInfo");
            }

            last4Digits = last4Digits.PadLeft(16, '*').Insert(4, " ").Insert(9, " ").Insert(14, " ");

            /* Setup the view model with the gathered information */
            ConfirmOrderViewModel webOrder = new ConfirmOrderViewModel
            {
                FullName = memberInfo.FullName,
                PhoneNumber = memberInfo.PhoneNumber,
                Address = memberAddress.Address,
                ProvinceName = memberAddress.Province.Name,
                CountryName = memberAddress.Country.CountryName,
                CreditCardLast4Digits = last4Digits,
                Items = cartItems,
                ItemSubTotal = cartItems.Sum(ci => ci.ItemTotal)
            };

            webOrder.ShippingCost = shippingCostService.CalculateShippingCost(webOrder.ItemSubTotal, cart.Items);
            webOrder.TaxAmount = webOrder.ItemSubTotal * 
                (memberAddress.Province.ProvincialTaxRate + memberAddress.Country.FederalTaxRate);

            return View(webOrder);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> PlaceOrder(List<CartItem> items)
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
            Cart cart = await GetCartWithLoadedProductsAsync(memberId);

            if (!EnsureCartMatchesConfirmedCart(items, memberId, cart))
            {
                this.AddAlert(AlertType.Warning, "Your cart changed between confirming it and placing the order.");

                return RedirectToAction("ConfirmOrder");
            }

            /* Setup the address information */
            MemberAddress memberAddress = await GetMemberAddress(orderCheckoutDetails);

            if (memberAddress == null)
            {
                this.AddAlert(AlertType.Error, "The shipping address you selected could not be found.");

                return RedirectToAction("ShippingInfo");
            }

            /* Setup the credit card information */
            string last4Digits = await GetLast4DigitsAsync(orderCheckoutDetails, memberId);

            if (last4Digits == null)
            {
                this.AddAlert(AlertType.Error, "The billing information you selected could not be found.");

                return RedirectToAction("BillingInfo");
            }

            Member currentMember = await db.Members.FindAsync(memberId);
            string stripeCardToken = await GetStripeCardToken(orderCheckoutDetails, memberId);

            decimal orderTotal = CalculateOrderTotal(cart,
                memberAddress.Province.ProvincialTaxRate,
                memberAddress.Country.FederalTaxRate);

            // TODO: This could throw
            string stripeChargeId = stripeService.ChargeCard(orderTotal, stripeCardToken, currentMember.StripeCustomerId);

            WebOrder newOrder = new WebOrder
            {
                OrderItems = new List<OrderItem>(),
                Address = memberAddress.Address,
                ProvinceCode = memberAddress.ProvinceCode,
                CountryCode = memberAddress.CountryCode,
                MemberId = memberId,
                CreditCardLast4Digits = last4Digits,
                OrderDate = DateTime.Now,
                OrderStatus = OrderStatus.PendingProcessing,
                StripeChargeId = stripeChargeId
            };

            using (TransactionScope newOrderScope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                // TODO: Handle this throwing due to not enough used copies
                await DecreaseInventoryAndAddToOrder(cart, newOrder);

                db.WebOrders.Add(newOrder);

                // TODO: This might not clear out the cart, or might clear out items added while this is being processed
                // Clear out the cart
                cart.Items = new List<CartItem>();

                try
                {
                    await db.SaveChangesAsync();

                    newOrderScope.Complete();
                }
                catch (DbUpdateException ex)
                {
                    stripeService.RefundCharge(stripeChargeId);

                    this.AddAlert(AlertType.Error,
                        "An error occured while placing your order. Please try again.");

                    return RedirectToAction("ConfirmOrder");
                }
            }

            Session.Remove(OrderCheckoutDetailsKey);

            this.AddAlert(AlertType.Success, $"Successfully placed an order for {orderTotal:C}.");

            return RedirectToAction("Index", "Home");
        }

        private decimal CalculateOrderTotal(Cart cart, decimal provincialTaxRate, decimal federalTaxRate)
        {
            decimal cartTotal = cart.TotalCartItemsPrice;
            decimal shippingCost = shippingCostService.CalculateShippingCost(cartTotal, cart.Items);

            decimal orderTotal = 
                cart.TotalCartItemsPrice * 
                (1 + provincialTaxRate + federalTaxRate) +
                shippingCost;

            return orderTotal;
        }

        private bool EnsureCartMatchesConfirmedCart(List<CartItem> items, Guid memberId, Cart cart)
        {
            items = SortCartItems(items);
            items.ForEach(i => i.MemberId = memberId);

            return items.SequenceEqual(cart.Items, CartItem.CartItemComparer);
        }

        private async Task DecreaseInventoryAndAddToOrder(Cart cart, WebOrder newOrder)
        {
            foreach (var item in cart.Items)
            {
                // TODO: Confirm availability statuses

                ProductLocationInventory inventory = await db.ProductLocationInventories.
                    Where(
                        pli => pli.ProductId == item.ProductId &&
                            pli.Location.SiteName == Location.ONLINE_WAREHOUSE_NAME).
                    FirstOrDefaultAsync();

                if (item.IsNew)
                {
                    AvailabilityStatus itemStatus = item.Product.ProductAvailabilityStatus;

                    if (itemStatus == AvailabilityStatus.Available ||
                        itemStatus == AvailabilityStatus.PreOrder)
                    {
                        inventory.NewOnHand -= item.Quantity;
                    }
                    else if ((itemStatus == AvailabilityStatus.DiscontinuedByManufacturer ||
                            itemStatus == AvailabilityStatus.NotForSale) &&
                        item.Quantity > inventory.NewOnHand)
                    {
                        // TODO: Something better
                        throw new InvalidOperationException("Not enough copies of a discontinued game to guarentee we will be able to fulfill your order.");
                    }
                }
                else
                {
                    if (inventory.UsedOnHand < item.Quantity)
                    {
                        // TODO: Something better
                        throw new InvalidOperationException("Not enough used copies to guarentee we will be able to fulfill your order.");
                    }

                    inventory.UsedOnHand -= item.Quantity;
                }

                newOrder.OrderItems.Add(
                    new OrderItem
                    {
                        IsNew = item.IsNew,
                        ListPrice = item.IsNew ? item.Product.NewWebPrice : item.Product.UsedWebPrice.Value,
                        Product = item.Product,
                        ProductId = item.ProductId,
                        Quantity = item.Quantity
                    });
            }
        }

        private async Task<MemberAddress> GetMemberAddress(WebOrderCheckoutDetails orderCheckoutDetails)
        {
            if (orderCheckoutDetails.MemberAddressId != null)
            {
                return await db.MemberAddresses.
                    Include(ma => ma.Province).
                    Include(ma => ma.Country).
                    SingleOrDefaultAsync(ma => ma.Id == orderCheckoutDetails.MemberAddressId);
            }

            MemberAddress memberAddress = new MemberAddress
            {
                Address = orderCheckoutDetails.Address,
                ProvinceCode = orderCheckoutDetails.ProvinceCode,
                CountryCode = orderCheckoutDetails.CountryCode
            };

            memberAddress.Province = await db.Provinces.
                Include(p => p.Country).
                FirstOrDefaultAsync(
                    p => p.ProvinceCode == memberAddress.ProvinceCode &&
                        p.CountryCode == memberAddress.CountryCode);
            memberAddress.Country = memberAddress.Province.Country;

            return memberAddress;
        }

        private async Task<string> GetStripeCardToken(WebOrderCheckoutDetails orderCheckoutDetails, Guid memberId)
        {
            if (orderCheckoutDetails.MemberCreditCardId != null)
            {
                return await db.Members.
                    Where(m => m.UserId == memberId).
                    SelectMany(m => m.CreditCards).
                    Where(cc => cc.Id == orderCheckoutDetails.MemberCreditCardId.Value).
                    Select(cc => cc.StripeCardId).
                    SingleOrDefaultAsync();
            }

            return orderCheckoutDetails.StripeCardToken;
        }

        private Guid GetUserId()
        {
            return idGetter.GetUserId(User.Identity);
        }

        private async Task<string> GetLast4DigitsAsync(WebOrderCheckoutDetails orderCheckoutDetails, Guid memberId)
        {
            string last4Digits;

            if (orderCheckoutDetails.MemberCreditCardId != null)
            {
                last4Digits = await db.Members.
                    Where(m => m.UserId == memberId).
                    SelectMany(m => m.CreditCards).
                    Where(cc => cc.Id == orderCheckoutDetails.MemberCreditCardId.Value).
                    Select(cc => cc.Last4Digits).
                    SingleOrDefaultAsync();
            }
            else
            {
                // TODO: This can throw
                // TODO: This should probably throw if we fail due to backend issues and inform the user of it
                last4Digits = stripeService.GetLast4ForToken(orderCheckoutDetails.StripeCardToken);
            }

            return last4Digits;
        }

        private async Task<Cart> GetCartWithLoadedProductsAsync(Guid memberId)
        {
            Cart cart = await db.Carts.
                Where(m => m.MemberId == memberId).
                Include(c => c.Items).
                Include(c => c.Items.Select(ci => ci.Product)).
                SingleOrDefaultAsync();

            cart.Items = SortCartItems(cart.Items);

            return cart;
        }

        private List<CartItem> SortCartItems(IEnumerable<CartItem> cartItems)
        {
            return cartItems.
                OrderByDescending(ci => ci.ProductId).
                ToList();
        } 

        private List<ConfirmOrderCartItem> GetConfirmOrderCartItems(Cart cart)
        {
            List<ConfirmOrderCartItem> cartItems =
                cart.Items.
                    Select(
                        ci =>
                            new ConfirmOrderCartItem
                            {
                                ProductId = ci.ProductId,
                                IsNew = ci.IsNew,
                                ItemPrice = ci.IsNew ? ci.Product.NewWebPrice : ci.Product.UsedWebPrice.Value,
                                Name = ci.Product.Name,
                                PlatformName =
                                    ci.Product is PhysicalGameProduct
                                        ? ((PhysicalGameProduct)ci.Product).Platform.PlatformName
                                        : "",
                                Quantity = ci.Quantity
                            }
                    ).ToList();

            return cartItems;
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