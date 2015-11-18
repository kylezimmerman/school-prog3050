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
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Transactions;
using System.Web.Mvc;
using System.Web.Routing;
using Microsoft.AspNet.Identity;
using Stripe;
using Veil.DataAccess.Interfaces;
using Veil.DataModels.Models;
using Veil.Exceptions;
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
        private readonly IIdentityMessageService emailService;

        public CheckoutController(IVeilDataAccess veilDataAccess, IGuidUserIdGetter idGetter, IStripeService stripeService, IShippingCostService shippingCostService, IIdentityMessageService emailService)
        {
            db = veilDataAccess;
            this.idGetter = idGetter;
            this.stripeService = stripeService;
            this.shippingCostService = shippingCostService;
            this.emailService = emailService;
        }

        [HttpGet]
        public async Task<ActionResult> ShippingInfo()
        {
            Guid memberId = GetUserId();

            ActionResult redirectToAction = await EnsureCartNotEmptyAsync(memberId);

            if (redirectToAction != null)
            {
                return redirectToAction;
            }

            AddressViewModel viewModel = new AddressViewModel();

            await viewModel.SetupAddressesAndCountries(db, memberId);

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
            Guid memberId = GetUserId();

            ActionResult redirectToAction = await EnsureCartNotEmptyAsync(memberId);
            if (redirectToAction != null)
            {
                return redirectToAction;
            }

            if (!ModelState.IsValid)
            {
                model.UpdatePostalCodeModelError(ModelState);

                this.AddAlert(AlertType.Error, "Some address information was invalid.");

                await model.SetupAddressesAndCountries(db, memberId);

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
                await model.SetupAddressesAndCountries(db, memberId);

                return View("ShippingInfo", model);
            }

            WebOrderCheckoutDetails orderCheckoutDetails = 
                Session[OrderCheckoutDetailsKey] as WebOrderCheckoutDetails ?? new WebOrderCheckoutDetails();

            model.FormatPostalCode();

            if (saveAddress)
            {
                MemberAddress newAddress = new MemberAddress
                {
                    MemberId = memberId,
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
            Guid memberId = GetUserId();

            ActionResult redirectToAction = await EnsureCartNotEmptyAsync(memberId);
            if (redirectToAction != null)
            {
                return redirectToAction;
            }

            if (!await db.MemberAddresses.AnyAsync(ma => ma.Id == addressId))
            {
                AddressViewModel model = new AddressViewModel();
                await model.SetupAddressesAndCountries(db, memberId);

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
            Guid memberId = GetUserId();

            ActionResult redirectToAction = await EnsureCartNotEmptyAsync(memberId);
            if (redirectToAction != null)
            {
                return redirectToAction;
            }

            WebOrderCheckoutDetails orderCheckoutDetails =
                Session[OrderCheckoutDetailsKey] as WebOrderCheckoutDetails;

            ActionResult invalidSessionResult = EnsureValidSessionForBillingStep(orderCheckoutDetails);

            if (invalidSessionResult != null)
            {
                return invalidSessionResult;
            }

            BillingInfoViewModel viewModel = new BillingInfoViewModel();

            await viewModel.SetupCreditCardsAndCountries(db, memberId);

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> NewBillingInfo(string stripeCardToken, bool saveCard)
        {
            Guid memberId = GetUserId();

            ActionResult redirectToAction = await EnsureCartNotEmptyAsync(memberId);
            if (redirectToAction != null)
            {
                return redirectToAction;
            }

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

                Member currentMember = await db.Members.FindAsync(memberId);

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
            Guid memberId = GetUserId();

            ActionResult redirectToAction = await EnsureCartNotEmptyAsync(memberId);
            if (redirectToAction != null)
            {
                return redirectToAction;
            }

            WebOrderCheckoutDetails orderCheckoutDetails =
                Session[OrderCheckoutDetailsKey] as WebOrderCheckoutDetails;

            ActionResult invalidSessionResult = EnsureValidSessionForBillingStep(orderCheckoutDetails);

            if (invalidSessionResult != null)
            {
                return invalidSessionResult;
            }

            Contract.Assume(orderCheckoutDetails != null);

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
            Guid memberId = GetUserId();

            ActionResult redirectToAction = await EnsureCartNotEmptyAsync(memberId);
            if (redirectToAction != null)
            {
                return redirectToAction;
            }

            WebOrderCheckoutDetails orderCheckoutDetails =
                Session[OrderCheckoutDetailsKey] as WebOrderCheckoutDetails;

            ActionResult invalidSessionResult = EnsureValidSessionForConfirmStep(orderCheckoutDetails);

            if (invalidSessionResult != null)
            {
                return invalidSessionResult;
            }

            Contract.Assume(orderCheckoutDetails != null);

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
            // Steps:
            // Confirm session is in a valid state to place the order
            // Confirm the cart matches the one the user placed an order for
            // Get the shipping info
            // Get the last 4 digits of the card for the order record
            // Get the stripe card token to be charged
            // Calculate the order total including taxes and shipping
            // Charge the stripe token
            // Create a new order with the address information, last 4 digits, charge token, memberId, order date, and order status
            // Decrease inventory levels and add the item to the web order
            // Clear the cart
            // Saves changes
            // If any exceptions occur, refund the charge
            // If no exceptions occur, clear out the session item for the order and send a order confirmation email

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

            if (cart.Items.Count == 0)
            {
                this.AddAlert(AlertType.Error, "You can't place an order with an empty cart.");

                return RedirectToAction("Index", "Cart");
            }

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

            decimal cartTotal = Math.Round(cart.TotalCartItemsPrice, 2);
            decimal shippingCost = shippingCostService.CalculateShippingCost(cartTotal, cart.Items);
            decimal taxAmount = Math.Round(cartTotal * (memberAddress.Province.ProvincialTaxRate + memberAddress.Country.FederalTaxRate), 2);

            decimal orderTotal = cart.TotalCartItemsPrice + taxAmount + shippingCost;

            string stripeChargeId;

            try
            {
                stripeChargeId = stripeService.ChargeCard(
                    orderTotal, stripeCardToken, currentMember.StripeCustomerId);
            }
            catch (StripeException ex)
            {
                // TODO: We would want to log this
                this.AddAlert(AlertType.Error, "An error occured while talking to one of our backends. Sorry!");

                return RedirectToAction("BillingInfo");
            }

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
                StripeChargeId = stripeChargeId,
                TaxAmount = taxAmount,
                ShippingCost = shippingCost,
                OrderSubtotal = cartTotal
            };

            using (TransactionScope newOrderScope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                try
                {
                    await DecreaseInventoryAndAddToOrder(cart, newOrder);
                }
                catch (NotEnoughInventoryException ex)
                {
                    this.AddAlert(AlertType.Error, ex.Message);

                    stripeService.RefundCharge(stripeChargeId);

                    return RedirectToAction("ConfirmOrder");
                }

                db.WebOrders.Add(newOrder);

                // This only clears out the cart as we have it. 
                // Anything added during this method's execution will remain in the cart.
                // I consider this to be the desired outcome.
                cart.Items.Clear();

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
            Session[CartController.CART_QTY_SESSION_KEY] = null;

            string orderDetailLink = HtmlHelper.GenerateLink(
                ControllerContext.RequestContext,
                RouteTable.Routes,
                "View Order",
                null,
                "Details",
                "WebOrders",
                new RouteValueDictionary(new { id = newOrder.Id }),
                null);

            this.AddAlert(AlertType.Success, $"Successfully placed an order for {orderTotal:C}. ", orderDetailLink);

            string to = currentMember.UserAccount.Email;
            string subject = $"Veil Order Confirmation - # {newOrder.Id}";

            IdentityMessage email = new IdentityMessage
            {
                Body = RenderRazorPartialViewToString("~/Views/WebOrders/_OrderConfirmationEmail.cshtml", newOrder),
                Destination = to,
                Subject = subject
            };

            await emailService.SendAsync(email);

            return RedirectToAction("Index", "Home");
        }

        private string RenderRazorPartialViewToString(string viewName, object model)
        {
            ViewData.Model = model;
            using (var sw = new StringWriter())
            {
                var viewResult = ViewEngines.Engines.FindPartialView(ControllerContext, viewName);
                var viewContext = new ViewContext(ControllerContext, viewResult.View, ViewData, TempData, sw);
                viewResult.View.Render(viewContext, sw);
                viewResult.ViewEngine.ReleaseView(ControllerContext, viewResult.View);
                return sw.GetStringBuilder().ToString();
            }
        }

        /// <summary>
        ///     Gets the Guid id of the current user
        /// </summary>
        /// <returns>
        ///     Guid id of the current user
        /// </returns>
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
                        throw new NotEnoughInventoryException(
                            $"Not enough copies of {item.Product.Name} which has been discontinued to " +
                                "guarantee we will be able to fulfill your order.",
                            item.Product);
                    }
                }
                else
                {
                    if (inventory.UsedOnHand < item.Quantity)
                    {
                        throw new NotEnoughInventoryException(
                            $"Not enough used copies of {item.Product.Name} to guarantee we " +
                                "will be able to fulfill your order.",
                            item.Product);
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

        private async Task<ActionResult> EnsureCartNotEmptyAsync(Guid memberId)
        {
            int cartQuantity = await db.Carts.
                Where(c => c.MemberId == memberId).
                Select(c => c.Items.Count).
                SingleOrDefaultAsync();

            if (cartQuantity == 0)
            {
                this.AddAlert(AlertType.Error, "You can't place an order with an empty cart.");

                return RedirectToAction("Index", "Cart");
            }

            return null;
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