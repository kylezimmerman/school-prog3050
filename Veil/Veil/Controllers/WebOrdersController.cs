using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using Stripe;
using Veil.DataAccess.Interfaces;
using Veil.DataModels.Models;
using Veil.Extensions;
using Veil.Helpers;
using Veil.Services.Interfaces;

namespace Veil.Controllers
{
    [Authorize]
    public class WebOrdersController : BaseController
    {
        private readonly IVeilDataAccess db;
        private readonly IGuidUserIdGetter idGetter;
        private readonly IStripeService stripeService;

        public WebOrdersController(IVeilDataAccess veilDataAccess, IGuidUserIdGetter idGetter,
            IStripeService stripeService)
        {
            db = veilDataAccess;
            this.idGetter = idGetter;
            this.stripeService = stripeService;
        }

        // GET: WebOrders
        /// <summary>
        ///     Displays a list of orders
        /// </summary>
        /// <returns>
        ///     Index view with the current member's orders
        ///     Index_Employee view with all unprocessed orders
        /// </returns>
        public async Task<ActionResult> Index()
        {
            IEnumerable<WebOrder> model;

            if (User.IsEmployeeOrAdmin())
            {
                model = await db.WebOrders
                    .Where(wo => wo.OrderStatus == OrderStatus.PendingProcessing)
                    .OrderBy(wo => wo.OrderDate).ToListAsync();
                return View("Index_Employee", model);
            }

            Guid memberId = idGetter.GetUserId(User.Identity);
            model = await db.WebOrders
                .Where(wo => wo.MemberId == memberId)
                .OrderByDescending(wo => wo.OrderDate).ToListAsync();

            return View(model);
        }

        // GET: WebOrders/Details/5
        /// <summary>
        ///     Displays information about a specific order
        /// </summary>
        /// <param name="id">
        ///     The id of the order to view details of
        /// </param>
        /// <returns>
        ///     Details view for the Order matching id
        ///     404 Not Found view if the id does not match an order
        ///     404 Not Found view if the current user is not the owner of the order and also not an employee
        /// </returns>
        public async Task<ActionResult> Details(long? id)
        {
            if (id == null)
            {
                throw new HttpException(NotFound, nameof(WebOrder));
            }

            WebOrder webOrder = await db.WebOrders.Include(wo => wo.Member).FirstOrDefaultAsync(wo => wo.Id == id);

            if (webOrder == null)
            {
                throw new HttpException(NotFound, nameof(WebOrder));
            }

            if (!User.IsEmployeeOrAdmin() &&
                webOrder.MemberId != idGetter.GetUserId(User.Identity))
            {
                throw new HttpException(NotFound, nameof(WebOrder));
            }

            return View(webOrder);
        }

        // POST: WebOrders/CancelOrder/5
        /// <summary>
        ///     Cancels an order and refunds payment
        /// </summary>
        /// <param name="id">
        ///     The id of the order to cancel
        /// </param>
        /// <returns>
        ///     Details view for the cancelled Order matching id
        ///     404 Not Found view if the id does not match an order
        ///     404 Not Found view if the current user is not the owner of the order
        ///     Details view with error if the order is being or has been processed
        ///     Details view with error if the order cancellation or refund fails
        /// </returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Cancel(long? id)
        {
            if (id == null)
            {
                throw new HttpException(NotFound, nameof(WebOrder));
            }

            WebOrder webOrder = await db.WebOrders.Include(wo => wo.Member).FirstOrDefaultAsync(wo => wo.Id == id);

            if (webOrder == null)
            {
                throw new HttpException(NotFound, nameof(WebOrder));
            }

            if (webOrder.MemberId != idGetter.GetUserId(User.Identity))
            {
                throw new HttpException(NotFound, nameof(WebOrder));
            }

            if (webOrder.OrderStatus != OrderStatus.PendingProcessing)
            {
                this.AddAlert(AlertType.Error,
                    "This order could not be cancelled. Only orders that are pending processing can be cancelled.");
            }
            else
            {
                await CancelAndRefundOrder(webOrder);
            }

            return RedirectToAction("Details", new { id = webOrder.Id });
        }

        private async Task CancelAndRefundOrder(WebOrder order)
        {
            using (var refundScope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                order.OrderStatus = OrderStatus.UserCancelled;
                order.ReasonForCancellationMessage = "Order cancelled by customer.";
                await RestoreInventoryOnCancellation(order);
                db.MarkAsModified(order);

                try
                {
                    await db.SaveChangesAsync();
                    stripeService.RefundCharge(order.StripeChargeId);

                    this.AddAlert(AlertType.Success, "Your order has been cancelled and payment refunded.");
                    refundScope.Complete();
                }
                catch (Exception ex) when (ex is DataException || ex is StripeException)
                {
                    string customerSupportLink = HtmlHelper.GenerateLink(
                        ControllerContext.RequestContext,
                        RouteTable.Routes,
                        "customer support.",
                        null,
                        "Contact",
                        "Home",
                        null,
                        null);

                    string errorMessage;

                    if (ex is DataException)
                    {
                        errorMessage =
                            "An error occurred cancelling the order. Your payment has not been refunded. Please try again. If this issue persists please contact ";
                    }
                    else
                    {
                        errorMessage =
                            "An error occurred refunding your payment. Please try again. If this issue persists please contact ";
                    }

                    this.AddAlert(AlertType.Error, errorMessage, customerSupportLink);
                }
            }
        }
        
        /// <summary>
        ///     Adds the items in a cancelled order back to the OnHand inventory levels
        /// </summary>
        /// <param name="order">
        ///     The order being cancelled
        /// </param>
        /// <returns>
        ///     A Task to await
        /// </returns>
        private async Task RestoreInventoryOnCancellation(WebOrder order)
        {
            foreach (var item in order.OrderItems)
            {
                ProductLocationInventory inventory = await db.ProductLocationInventories.
                    Where(
                        pli => pli.ProductId == item.ProductId &&
                            pli.Location.SiteName == Location.ONLINE_WAREHOUSE_NAME).
                    FirstOrDefaultAsync();

                if (item.IsNew)
                {
                    inventory.NewOnHand += item.Quantity;
                }
                else
                {
                    inventory.UsedOnHand += item.Quantity;
                }
            }
        }
    }
}
