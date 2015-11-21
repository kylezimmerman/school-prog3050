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
using Veil.DataModels;
using Veil.DataModels.Models;
using Veil.Extensions;
using Veil.Helpers;
using Veil.Services;
using Veil.Services.Interfaces;

namespace Veil.Controllers
{
    [Authorize]
    public class WebOrdersController : BaseController
    {
        private readonly IVeilDataAccess db;
        private readonly IGuidUserIdGetter idGetter;
        private readonly IStripeService stripeService;
        private readonly VeilUserManager userManager;

        public WebOrdersController(IVeilDataAccess veilDataAccess, IGuidUserIdGetter idGetter,
            IStripeService stripeService, VeilUserManager userManager)
        {
            db = veilDataAccess;
            this.idGetter = idGetter;
            this.stripeService = stripeService;
            this.userManager = userManager;
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
        ///     The customer cancels an order they made
        /// </summary>
        /// <param name="id">
        ///     The id of the order to cancel
        /// </param>
        /// <returns>
        ///     Details view for the cancelled Order matching id
        ///     Details view with error if the order is being or has been processed
        ///     Details view with error if the order cancellation or refund fails
        ///     404 Not Found view if the current user is not the owner of the order
        /// </returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = VeilRoles.MEMBER_ROLE)]
        public async Task<ActionResult> Cancel(long? id)
        {
            WebOrder webOrder = await GetOrder(id);

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
                await CancelAndRefundOrder(webOrder, "Order cancelled by customer.");
            }

            return RedirectToAction("Details", new { id = webOrder.Id });
        }

        /// <summary>
        ///     An employee changes the status of an order to EmployeeCancelled
        /// </summary>
        /// <param name="id">
        ///     The id of the order to cancel
        /// </param>
        /// <param name="reasonForCancellation">
        ///     If newStatus is EmployeeCancelled, the reason the order is being cancelled
        /// </param>
        /// <returns>
        ///     Details view for the modified Order
        ///     Details view with error if the order is being cancelled and a reason has not been supplied
        /// </returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = VeilRoles.Authorize.Admin_Employee)]
        public async Task<ActionResult> SetStatusCancelled(long? id, string reasonForCancellation)
        {
            if (string.IsNullOrWhiteSpace(reasonForCancellation))
            {
                this.AddAlert(AlertType.Error, "You must provide a reason for cancellation.");

                return RedirectToAction("Details", new { id = id });
            }

            WebOrder webOrder = await GetOrder(id);

            if (webOrder.OrderStatus != OrderStatus.PendingProcessing &&
                webOrder.OrderStatus != OrderStatus.BeingProcessed)
            {
                this.AddAlert(AlertType.Error, "You can only cancel an order if it is pending processing or being processed.");
            }
            else
            {
                await CancelAndRefundOrder(webOrder, reasonForCancellation);
            }

            return RedirectToAction("Details", new { id = webOrder.Id });
        }

        /// <summary>
        ///     An employee changes the status of an order to BeingProcessed
        /// </summary>
        /// <param name="id">
        ///     The id of the order to modify
        /// </param>
        /// <returns>
        ///     Details view for the modified Order
        ///     Details view with error if the order is not PendingProcessing
        /// </returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = VeilRoles.Authorize.Admin_Employee)]
        public async Task<ActionResult> SetStatusProcessing(long? id)
        {
            WebOrder webOrder = await GetOrder(id);

            if (webOrder.OrderStatus != OrderStatus.PendingProcessing)
            {
                this.AddAlert(AlertType.Error, "An order can only begin processing if its status is Pending Processing.");
            }
            else
            {
                webOrder.OrderStatus = OrderStatus.BeingProcessed;
                await db.SaveChangesAsync();
            }

            return RedirectToAction("Details", new { id = webOrder.Id });
        }

        /// <summary>
        ///     An employee changes the status of an order to Processed
        /// </summary>
        /// <param name="id">
        ///     The id of the order to modify
        /// </param>
        /// <returns>
        ///     Details view for the modified Order
        ///     Details view with error if the order is not BeingProcessed
        /// </returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = VeilRoles.Authorize.Admin_Employee)]
        public async Task<ActionResult> SetStatusProcessed(long? id)
        {
            WebOrder webOrder = await GetOrder(id);

            if (webOrder.OrderStatus != OrderStatus.BeingProcessed)
            {
                this.AddAlert(AlertType.Error, "An order can only be processed if its status is Being Processed.");
            }
            else
            {
                webOrder.OrderStatus = OrderStatus.Processed;
                webOrder.ProcessedDate = DateTime.Now;
                await db.SaveChangesAsync();

                string subject = $"Veil Order Processed - # {webOrder.Id}";
                string body = RenderRazorPartialViewToString("_OrderProcessedEmail", webOrder);

                await userManager.SendEmailAsync(webOrder.MemberId, subject, body);
            }

            return RedirectToAction("Details", new { id = webOrder.Id });
        }

        /// <summary>
        ///     An order is cancelled and payment is refunded
        /// </summary>
        /// <param name="order">
        ///     The order to be cancelled
        /// </param>
        /// <param name="reasonForCancellation">
        ///     The reason the order is being cancelled
        /// </param>
        /// <returns>
        ///     A Task to await
        /// </returns>
        private async Task CancelAndRefundOrder(WebOrder order, string reasonForCancellation)
        {
            using (var refundScope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                order.OrderStatus = OrderStatus.UserCancelled;
                order.ReasonForCancellationMessage = reasonForCancellation;
                await RestoreInventoryOnCancellation(order);
                db.MarkAsModified(order);

                try
                {
                    await db.SaveChangesAsync();
                    stripeService.RefundCharge(order.StripeChargeId);

                    this.AddAlert(AlertType.Success, "The order has been cancelled and payment refunded.");
                    refundScope.Complete();

                    string subject = $"Veil Order Cancelled - # {order.Id}";
                    string body = RenderRazorPartialViewToString("_OrderCancellationEmail", order);

                    await userManager.SendEmailAsync(order.MemberId, subject, body);
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
                            "An error occurred cancelling the order. Payment has not been refunded. Please try again. If this issue persists please contact ";
                    }
                    else
                    {
                        errorMessage =
                            "An error occurred refunding payment. Please try again. If this issue persists please contact ";
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

        /// <summary>
        ///     Gets the order with specified id while checking for nulls
        /// </summary>
        /// <param name="id">
        ///     The id of the WebOrder to return
        /// </param>
        /// <returns>
        ///     The WebOrder matching id
        ///     404 Not Found view if the id does not match an order
        /// </returns>
        private async Task<WebOrder> GetOrder(long? id)
        {
            if (id == null)
            {
                throw new HttpException(NotFound, nameof(WebOrder));
            }

            WebOrder webOrder = await db.WebOrders.FindAsync(id.Value);

            if (webOrder == null)
            {
                throw new HttpException(NotFound, nameof(WebOrder));
            }

            return webOrder;
        }
    }
}
