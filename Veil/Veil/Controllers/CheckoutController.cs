/* CheckoutController.cs
 * Purpose: Controller for processing order checkout
 * 
 * Revision History:
 *      Drew Matheson, 2015.10.13: Created
 */ 

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Transactions;
using System.Web.Mvc;
using System.Web.Routing;
using JetBrains.Annotations;
using Stripe;
using Veil.DataAccess.Interfaces;
using Veil.DataModels;
using Veil.DataModels.Models;
using Veil.Exceptions;
using Veil.Extensions;
using Veil.Helpers;
using Veil.Models;
using Veil.Services;
using Veil.Services.Interfaces;
using static System.Math;

namespace Veil.Controllers
{
    /// <summary>
    ///     Controller for the order checkout steps
    /// </summary>
    [Authorize(Roles = VeilRoles.MEMBER_ROLE)]
    public class CheckoutController : BaseController
    {
        public static string OrderCheckoutDetailsKey = "CheckoutController.OrderCheckoutDetails";

        private readonly IVeilDataAccess db;
        private readonly IGuidUserIdGetter idGetter;
        private readonly IStripeService stripeService;
        private readonly IShippingCostService shippingCostService;
        private readonly VeilUserManager userManager;

        public CheckoutController(IVeilDataAccess veilDataAccess, IGuidUserIdGetter idGetter,
            IStripeService stripeService, IShippingCostService shippingCostService,
            VeilUserManager userManager)
        {
            db = veilDataAccess;
            this.idGetter = idGetter;
            this.stripeService = stripeService;
            this.shippingCostService = shippingCostService;
            this.userManager = userManager;
        }

        /// <summary>
        ///     Displays the shipping information entry page
        /// </summary>
        /// <returns>
        ///     The shipping information page if successful
        ///     Redirection to Cart/Index if the cart is empty
        /// </returns>
        [HttpGet]
        public async Task<ActionResult> ShippingInfo()
        {
            Guid memberId = GetUserId();

            RedirectToRouteResult invalidStateResult = await EnsureCartNotEmptyAsync(memberId);
            if (invalidStateResult != null)
            {
                return invalidStateResult;
            }

            var viewModel = new AddressViewModel();
            await viewModel.SetupAddressesAndCountries(db, memberId);

            var orderCheckoutDetails = Session[OrderCheckoutDetailsKey] as WebOrderCheckoutDetails;

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

        /// <summary>
        ///     Adds new shipping information to the order details and 
        ///     forwards the user to the billing info step
        /// </summary>
        /// <param name="model">
        ///     The view model containing the new address info
        /// </param>
        /// <param name="saveAddress">
        ///     bool indicating if the address should be saved
        /// </param>
        /// <param name="returnToConfirm">
        ///     True if the user got here from the ConfirmOrder page
        /// </param>
        /// <returns>
        ///     Redirection to BillingInfo if successful
        ///     Redirection to Cart/Index if the cart is empty
        ///     Redisplays the page if the any information is invalid
        ///     Redirects to ConfirmOrder if returnToConfirm is true
        /// </returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> NewShippingInfo(AddressViewModel model, bool saveAddress,
            bool returnToConfirm = false)
        {
            Guid memberId = GetUserId();

            RedirectToRouteResult invalidStateResult = await EnsureCartNotEmptyAsync(memberId);
            if (invalidStateResult != null)
            {
                return invalidStateResult;
            }

            if (!ModelState.IsValid)
            {
                model.UpdatePostalCodeModelError(ModelState);

                this.AddAlert(AlertType.Error, "Some address information was invalid.");

                await model.SetupAddressesAndCountries(db, memberId);

                return View("ShippingInfo", model);
            }

            bool validCountry = await db.Countries.AnyAsync(c => c.CountryCode == model.CountryCode);
            bool validProvince = true;

            if (!validCountry)
            {
                this.AddAlert(AlertType.Error, "The Country you selected isn't valid.");
            }
            else
            {
                validProvince = await db.Provinces.
                AnyAsync(p => p.CountryCode == model.CountryCode &&
                        p.ProvinceCode == model.ProvinceCode);

                if (!validProvince)
                {
                    this.AddAlert(AlertType.Error,
                        "The Province/State you selected isn't in the Country you selected.");
                }
            }

            if (!validCountry || !validProvince)
            {
                await model.SetupAddressesAndCountries(db, memberId);

                return View("ShippingInfo", model);
            }

            var orderCheckoutDetails =  Session[OrderCheckoutDetailsKey] as WebOrderCheckoutDetails ??
                new WebOrderCheckoutDetails();

            model.FormatPostalCode();

            if (saveAddress)
            {
                var newAddress = new MemberAddress
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

        /// <summary>
        ///     Adds existing shipping information to the order details and forwards the user
        ///     to the billing info step
        /// </summary>
        /// <param name="addressId">
        ///     The <see cref="MemberAddress.Id"/> of the <see cref="MemberAddress"/> to use
        /// </param>
        /// <param name="returnToConfirm">
        ///     True if the user got here from the ConfirmOrder page
        /// </param>
        /// <returns>
        ///     Redirection to BillingInfo if successful
        ///     Redirection to Cart/Index if the cart is empty
        ///     Redirects to ShippingInfo if the address can't be found
        ///     Redirects to ConfirmOrder if returnToConfirm is true
        /// </returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ExistingShippingInfo(Guid addressId, bool returnToConfirm = false)
        {
            Guid memberId = GetUserId();

            ActionResult invalidStateResult = await EnsureCartNotEmptyAsync(memberId);
            if (invalidStateResult != null)
            {
                return invalidStateResult;
            }

            if (!await db.MemberAddresses.AnyAsync(ma => ma.Id == addressId))
            {
                this.AddAlert(AlertType.Error, "The address you selected could not be found.");

                return RedirectToAction("ShippingInfo");
            }

            var orderCheckoutDetails =  Session[OrderCheckoutDetailsKey] as WebOrderCheckoutDetails ??
                new WebOrderCheckoutDetails();

            orderCheckoutDetails.MemberAddressId = addressId;
            
            Session[OrderCheckoutDetailsKey] = orderCheckoutDetails;

            if (returnToConfirm)
            {
                return RedirectToAction("ConfirmOrder");
            }

            return RedirectToAction("BillingInfo");
        }

        /// <summary>
        ///     Displays the billing information entry page
        /// </summary>
        /// <returns>
        ///     The billing information entry page if successful
        ///     Redirects to Cart/Index if the cart is empty
        ///     Redirects to ShippingInfo if shipping info is unset
        /// </returns>
        [HttpGet]
        public async Task<ActionResult> BillingInfo()
        {
            Guid memberId = GetUserId();

            ActionResult invalidStateResult = await EnsureCartNotEmptyAsync(memberId);
            if (invalidStateResult != null)
            {
                return invalidStateResult;
            }

            var orderCheckoutDetails = Session[OrderCheckoutDetailsKey] as WebOrderCheckoutDetails;

            invalidStateResult = EnsureValidSessionForBillingStep(orderCheckoutDetails);
            if (invalidStateResult != null)
            {
                return invalidStateResult;
            }

            var viewModel = new BillingInfoViewModel();
            await viewModel.SetupCreditCardsAndCountries(db, memberId);
            return View("BillingInfo", viewModel);
        }

        /// <summary>
        ///     Adds new billing information to the order details and 
        ///     forwards the user to confirm their order
        /// </summary>
        /// <param name="stripeCardToken">
        ///     The Stripe Card Token for the new card
        /// </param>
        /// <param name="saveCard">
        ///     bool indicating if the card should be saved to the Member's Stripe Customer account
        /// </param>
        /// <returns>
        ///     Redirection to ConfirmOrder if successful
        ///     Redirection to Cart/Index if the cart is empty
        ///     Redirection to ShippingInfo if shipping info is unset
        ///     Redisplay the page if data is invalid or Stripe throws
        /// </returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> NewBillingInfo(string stripeCardToken, bool saveCard)
        {
            Guid memberId = GetUserId();

            RedirectToRouteResult invalidStateResult = await EnsureCartNotEmptyAsync(memberId);
            if (invalidStateResult != null)
            {
                return invalidStateResult;
            }

            var orderCheckoutDetails = Session[OrderCheckoutDetailsKey] as WebOrderCheckoutDetails;

            invalidStateResult = EnsureValidSessionForBillingStep(orderCheckoutDetails);
            if (invalidStateResult != null)
            {
                return invalidStateResult;
            }

            Contract.Assume(orderCheckoutDetails != null);

            if (string.IsNullOrWhiteSpace(stripeCardToken))
            {
                this.AddAlert(AlertType.Error, "Some credit card information is invalid.");

                return RedirectToAction("BillingInfo");
            }

            if (saveCard)
            {
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
                        ModelState.AddModelError(ManageController.STRIPE_ISSUES_MODELSTATE_KEY,
                            ex.Message);
                    }
                    else
                    {
                        this.AddAlert(AlertType.Error,
                            "An error occured while talking to one of our backends. Sorry!");
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

        /// <summary>
        ///     Adds an existing card to the order details and forwards the user to confirm their order
        /// </summary>
        /// <param name="cardId">
        ///     The <see cref="MemberCreditCard.Id"/> of the <see cref="MemberCreditCard"/> to use
        /// </param>
        /// <returns>
        ///     Redirection to ConfirmOrder if successful
        ///     Redirection to Cart/Index if the cart is empty
        ///     Redirection to ShippingInfo if shipping info is unset
        ///     Redisplay of the page if the cart can't be found
        /// </returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ExistingBillingInfo(Guid cardId)
        {
            Guid memberId = GetUserId();

            RedirectToRouteResult invalidStateResult = await EnsureCartNotEmptyAsync(memberId);
            if (invalidStateResult != null)
            {
                return invalidStateResult;
            }

            var orderCheckoutDetails = Session[OrderCheckoutDetailsKey] as WebOrderCheckoutDetails;

            invalidStateResult = EnsureValidSessionForBillingStep(orderCheckoutDetails);
            if (invalidStateResult != null)
            {
                return invalidStateResult;
            }

            Contract.Assume(orderCheckoutDetails != null);

            if (!await db.Members.
                    Where(m => m.UserId == memberId).
                    AnyAsync(m => m.CreditCards.Any(cc => cc.Id == cardId)))
            {
                this.AddAlert(AlertType.Error, "The card you selected could not be found.");

                return RedirectToAction("BillingInfo");
            }

            orderCheckoutDetails.MemberCreditCardId = cardId;

            Session[OrderCheckoutDetailsKey] = orderCheckoutDetails;

            return RedirectToAction("ConfirmOrder");
        }

        /// <summary>
        ///     Displays an order confirmation page which allows the user to place their order.
        /// </summary>
        /// <returns>
        ///     The order confirmation page if successful.
        ///     A redirection to another page if any information is invalid
        /// </returns>
        public async Task<ActionResult> ConfirmOrder()
        {
            Guid memberId = GetUserId();

            RedirectToRouteResult invalidStateResult = await EnsureCartNotEmptyAsync(memberId);
            if (invalidStateResult != null)
            {
                return invalidStateResult;
            }

            var orderCheckoutDetails = Session[OrderCheckoutDetailsKey] as WebOrderCheckoutDetails;

            invalidStateResult = EnsureValidSessionForConfirmStep(orderCheckoutDetails);
            if (invalidStateResult != null)
            {
                return invalidStateResult;
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

            /* Setup the address information */
            MemberAddress memberAddress = await GetShippingAddress(orderCheckoutDetails);
            if (memberAddress == null)
            {
                return RedirectToAction("ShippingInfo");
            }

            /* Setup the credit card information */
            string last4Digits = await GetLast4DigitsAsync(orderCheckoutDetails, memberId);
            if (last4Digits == null)
            {
                return RedirectToAction("BillingInfo");
            }

            Cart cart = await GetCartWithLoadedProductsAsync(memberId);
            var cartItems = GetConfirmOrderCartItems(cart);

            /* Setup the view model with the gathered information */
            var webOrder = new ConfirmOrderViewModel
            {
                FullName = memberInfo.FullName,
                PhoneNumber = memberInfo.PhoneNumber,
                Address = memberAddress.Address,
                ProvinceName = memberAddress.Province.Name,
                CountryName = memberAddress.Country.CountryName,
                CreditCardLast4Digits = last4Digits.FormatLast4Digits(),
                Items = cartItems,
                ItemSubTotal = cartItems.Sum(ci => ci.ItemTotal)
            };

            webOrder.ShippingCost = shippingCostService.CalculateShippingCost(webOrder.ItemSubTotal, cart.Items);
            webOrder.TaxAmount = webOrder.ItemSubTotal * 
                (memberAddress.Province.ProvincialTaxRate + memberAddress.Country.FederalTaxRate);

            return View(webOrder);
        }

        /// <summary>
        ///     Places the order
        /// </summary>
        /// <param name="items">
        ///     The <see cref="List{T}"/> of <see cref="CartItem"/>s that the used confirmed purchase of
        /// </param>
        /// <returns>
        ///     A redirection to Home/Index if successful.
        ///     A redirection to somewhere else if unsuccessful.
        /// </returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> PlaceOrder(List<CartItem> items)
        {
            // Steps:
            // Confirm session is in a valid state to place the order
            // Confirm the cart isn't empty
            // Confirm the cart matches the one the user placed an order for
            // Get the shipping info
            // Get the last 4 digits of the card for the order record
            // Get the stripe card token to be charged
            // Calculate the order total including taxes and shipping
            // Create a new order with the address information, last 4 digits, charge token, memberId, order date, and order status
            // Decrease inventory levels and add the item to the web order
            // Charge the stripe token
            // Clear the cart
            // Saves changes
            // If any exceptions occur, refund the charge
            // If no exceptions occur, clear out the session item for the order and send a order confirmation email

            var orderCheckoutDetails = Session[OrderCheckoutDetailsKey] as WebOrderCheckoutDetails;

            RedirectToRouteResult invalidSessionResult = 
                EnsureValidSessionForConfirmStep(orderCheckoutDetails);

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
                this.AddAlert(AlertType.Warning,
                    "Your cart changed between confirming it and placing the order.");

                return RedirectToAction("ConfirmOrder");
            }

            /* Setup the address information */
            MemberAddress memberAddress = await GetShippingAddress(orderCheckoutDetails);

            if (memberAddress == null)
            {
                return RedirectToAction("ShippingInfo");
            }

            /* Setup the credit card information */
            string last4Digits = await GetLast4DigitsAsync(orderCheckoutDetails, memberId);

            if (last4Digits == null)
            {
                return RedirectToAction("BillingInfo");
            }

            bool usingExistingCreditCard = orderCheckoutDetails.MemberCreditCardId != null;

            string memberStripeCustomerId = null;
            if (usingExistingCreditCard)
            {
                memberStripeCustomerId = await db.Members.
                    Where(m => m.UserId == memberId).
                    Select(m => m.StripeCustomerId).
                    SingleOrDefaultAsync();
            }

            string stripeCardToken = await GetStripeCardToken(orderCheckoutDetails, memberId);

            decimal cartTotal = Round(cart.TotalCartItemsPrice, 2);
            decimal shippingCost = shippingCostService.CalculateShippingCost(cartTotal, cart.Items);
            decimal taxAmount = Round(
                cartTotal *
                    (memberAddress.Province.ProvincialTaxRate + memberAddress.Country.FederalTaxRate),
                2);

            decimal orderTotal = cart.TotalCartItemsPrice + taxAmount + shippingCost;

            var order = new WebOrder
            {
                OrderItems = new List<OrderItem>(),
                Address = memberAddress.Address,
                ProvinceCode = memberAddress.ProvinceCode,
                CountryCode = memberAddress.CountryCode,
                MemberId = memberId,
                CreditCardLast4Digits = last4Digits,
                OrderDate = DateTime.Now,
                OrderStatus = OrderStatus.PendingProcessing,
                TaxAmount = taxAmount,
                ShippingCost = shippingCost,
                OrderSubtotal = cartTotal
            };

            using (var newOrderScope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                try
                {
                    await DecreaseInventoryAndAddToOrder(cart, order);
                }
                catch (NotEnoughInventoryException ex)
                {
                    this.AddAlert(AlertType.Error, ex.Message);

                    return RedirectToAction("ConfirmOrder");
                }

                string stripeChargeId;

                try
                {
                    stripeChargeId = stripeService.ChargeCard(
                        orderTotal, stripeCardToken, memberStripeCustomerId);

                    order.StripeChargeId = stripeChargeId;
                }
                catch (StripeException ex)
                {
                    // TODO: Look into the error returned due to a declined card
                    // TODO: We would want to log this
                    this.AddAlert(AlertType.Error,
                        "An error occured while talking to one of our backends. Sorry!");

                    return RedirectToAction("ConfirmOrder");
                }

                db.WebOrders.Add(order);

                // This only clears out the cart as we have it. 
                // Anything added during this method's execution will remain in the cart.
                // I consider this to be the desired outcome.
                cart.Items.Clear();

                try
                {
                    await db.SaveChangesAsync();

                    newOrderScope.Complete();
                }
                catch (DataException ex)
                {
                    stripeService.RefundCharge(stripeChargeId);

                    this.AddAlert(
                        AlertType.Error,
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
                new RouteValueDictionary(new { id = order.Id }),
                null);

            this.AddAlert(AlertType.Success,
                $"Successfully placed order #{order.Id} for {orderTotal:C}. ", orderDetailLink);

            await SendConfirmationEmailAsync(order, memberId);

            return RedirectToAction("Index", "Home");
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

        /// <summary>
        ///     Gets the last for digits for the card information in <see cref="orderCheckoutDetails"/>
        /// </summary>
        /// <param name="orderCheckoutDetails">
        ///     The <see cref="WebOrderCheckoutDetails"/> to use for retrieving the last 4 digits
        /// </param>
        /// <param name="memberId">
        ///     The id of the current member. Used if the information in 
        ///     <see cref="orderCheckoutDetails"/> is for a saved <see cref="MemberCreditCard"/>
        /// </param>
        /// <returns>
        ///     The last 4 card digits for the card associated with <see cref="orderCheckoutDetails"/>
        ///     If unsuccessful, null will be returned with an error alert already added.
        /// </returns>
        private async Task<string> GetLast4DigitsAsync(WebOrderCheckoutDetails orderCheckoutDetails,
            Guid memberId)
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
                try
                {
                    last4Digits = stripeService.GetLast4ForToken(orderCheckoutDetails.StripeCardToken);
                }
                catch (StripeException ex)
                {
                    // TODO: We would want to log this
                    this.AddAlert(AlertType.Error, "An error occured while talking to one of our backends. Sorry!");

                    return null;
                }
            }
            
            if (last4Digits == null)
            {
                this.AddAlert(AlertType.Error, "The billing information you selected could not be found.");
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

        /// <summary>
        ///     Sorts the <see cref="cartItems"/> by <see cref="CartItem.ProductId"/>
        /// </summary>
        /// <param name="cartItems">
        ///     The <see cref="IEnumerable{T}"/> of <see cref="CartItem"/>s to sort
        /// </param>
        /// <returns>
        ///     A <see cref="List{T}"/> of the sorted <see cref="CartItem"/>s
        /// </returns>
        private List<CartItem> SortCartItems(IEnumerable<CartItem> cartItems)
        {
            return cartItems.
                OrderByDescending(ci => ci.ProductId).
                ToList();
        }

        /// <summary>
        ///     Transforms the items in the <see cref="cart"/> to 
        ///     <see cref="ConfirmOrderCartItemViewModel"/>s
        /// </summary>
        /// <param name="cart">
        ///     The <see cref="Cart"/> whose items should be transformed
        /// </param>
        /// <returns>
        ///     A <see cref="List{T}"/> of <see cref="ConfirmOrderCartItemViewModel"/> for
        ///     the <see cref="cart"/>
        /// </returns>
        private List<ConfirmOrderCartItemViewModel> GetConfirmOrderCartItems(Cart cart)
        {
            List<ConfirmOrderCartItemViewModel> cartItems =
                cart.Items.
                    Select(
                        ci =>
                            new ConfirmOrderCartItemViewModel
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

        /// <summary>
        ///     Check if the passed <see cref="Cart"/> has the same items as <see cref="items"/>
        /// </summary>
        /// <param name="items">
        ///     The <see cref="List{T}"/> of <see cref="CartItem"/> the <see cref="cart"/> should match
        /// </param>
        /// <param name="memberId">
        ///     The id for the current member. This is used to set
        ///     the <see cref="CartItem.MemberId"/> of the cart items
        /// </param>
        /// <param name="cart">
        ///     The <see cref="Cart"/> with <see cref="Cart.Items"/> ordered by 
        ///     <see cref="SortCartItems"/> to compare against
        /// </param>
        /// <returns>
        ///     True if the items are all equal, false otherwise.
        /// </returns>
        private bool EnsureCartMatchesConfirmedCart(List<CartItem> items, Guid memberId, Cart cart)
        {
            items = SortCartItems(items);
            items.ForEach(i => i.MemberId = memberId);

            return items.SequenceEqual(cart.Items, CartItem.CartItemComparer);
        }

        /// <summary>
        ///     Decreases inventory levels and adds all the cart items to <see cref="newOrder"/>
        /// </summary>
        /// <param name="cart">
        ///     The <see cref="Cart"/> to retrieve items from
        /// </param>
        /// <param name="newOrder">
        ///     The <see cref="WebOrder"/> to add items to
        /// </param>
        /// <returns>
        ///     A <see cref="Task"/> to await
        /// </returns>
        private async Task DecreaseInventoryAndAddToOrder(Cart cart, WebOrder newOrder)
        {
            foreach (var item in cart.Items)
            {
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
                            $"Not enough copies of {item.Product.Name}, which has been discontinued, to " +
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

        /// <summary>
        ///     Gets a new <see cref="MemberAddress"/> populated with the shipping information
        ///     from the <see cref="orderCheckoutDetails"/>
        /// </summary>
        /// <param name="orderCheckoutDetails">
        ///     The <see cref="WebOrderCheckoutDetails"/> to use for retrieving the address information
        /// </param>
        /// <returns>
        ///     A new <see cref="MemberAddress"/> populated with the shipping address information.
        ///     If unsuccessful, null with be returned with an error alert already added
        /// </returns>
        private async Task<MemberAddress> GetShippingAddress(WebOrderCheckoutDetails orderCheckoutDetails)
        {
            MemberAddress memberAddress;

            if (orderCheckoutDetails.MemberAddressId != null)
            {
                memberAddress = await db.MemberAddresses.
                    Include(ma => ma.Province).
                    Include(ma => ma.Country).
                    SingleOrDefaultAsync(ma => ma.Id == orderCheckoutDetails.MemberAddressId);

                
            }
            else
            {
                memberAddress = new MemberAddress
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
            }

            if (memberAddress == null)
            {
                this.AddAlert(AlertType.Error, "The shipping address you selected could not be found.");
            }

            return memberAddress;
        }

        /// <summary>
        ///     Gets the Stripe Card Token from the <see cref="orderCheckoutDetails"/> for
        ///     the member identified by <see cref="memberId"/>
        /// </summary>
        /// <param name="orderCheckoutDetails">
        ///     The <see cref="WebOrderCheckoutDetails"/> to use for retrieving the Stripe Card Token
        /// </param>
        /// <param name="memberId">
        ///     The id for the current member
        /// </param>
        /// <returns>
        ///     The Stripe Card Token
        /// </returns>
        private async Task<string> GetStripeCardToken(WebOrderCheckoutDetails orderCheckoutDetails,
            Guid memberId)
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

        /// <summary>
        ///     Ensures the member identified by <see cref="memberId"/> does not have an empty card
        /// </summary>
        /// <param name="memberId">
        ///     The id for the current member
        /// </param>
        /// <returns>
        ///     null if the cart is not empty. 
        ///     Otherwise, a <see cref="RedirectToRouteResult"/> which should be returned
        /// </returns>
        [NonAction]
        protected virtual async Task<RedirectToRouteResult> EnsureCartNotEmptyAsync(Guid memberId)
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

        /// <summary>
        ///     Sends an order confirmation email to the user
        /// </summary>
        /// <param name="order">
        ///     The order to send a confirmation email for
        /// </param>
        /// <param name="memberId">
        ///     The id of the member to send the email to
        /// </param>
        /// <returns>
        ///     A task to await
        /// </returns>
        private async Task SendConfirmationEmailAsync(WebOrder order, Guid memberId)
        {
            string subject = $"Veil Order Confirmation - # {order.Id}";
            string body = RenderRazorPartialViewToString("_OrderConfirmationEmail", order);

            await userManager.SendEmailAsync(memberId, subject, body);
        }

        /// <summary>
        ///     Ensures the <see cref="checkoutDetails"/> is in a valid state for the billing step
        /// </summary>
        /// <param name="checkoutDetails">
        ///     The <see cref="WebOrderCheckoutDetails"/> to validate
        /// </param>
        /// <returns>
        ///     null if valid. Otherwise, a <see cref="RedirectToRouteResult"/> which should be returned
        /// </returns>
        [NonAction]
        private RedirectToRouteResult EnsureValidSessionForBillingStep(
            WebOrderCheckoutDetails checkoutDetails)
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

        /// <summary>
        ///     Ensures the <see cref="checkoutDetails"/> is in a valid state for the confirm order step
        /// </summary>
        /// <param name="checkoutDetails">
        ///     The <see cref="WebOrderCheckoutDetails"/> to validate
        /// </param>
        /// <returns>
        ///     null if valid. Otherwise, a <see cref="RedirectToRouteResult"/> which should be returned
        /// </returns>
        [NonAction]
        private RedirectToRouteResult EnsureValidSessionForConfirmStep(
            WebOrderCheckoutDetails checkoutDetails)
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