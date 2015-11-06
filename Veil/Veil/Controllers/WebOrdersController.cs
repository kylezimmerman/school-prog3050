using System;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Net;
using System.Web.Mvc;
using Veil.DataAccess.Interfaces;
using Veil.DataModels.Models;

namespace Veil.Controllers
{
    public class WebOrdersController : BaseController
    {
        private readonly IVeilDataAccess db;

        public WebOrdersController(IVeilDataAccess veilDataAccess)
        {
            db = veilDataAccess;
        }

        // GET: WebOrders
        public async Task<ActionResult> Index()
        {
            var webOrders = db.WebOrders.Include(w => w.Member).Include(w => w.MemberCreditCard).Include(w => w.ShippingAddress);
            return View(await webOrders.ToListAsync());
        }

        // GET: WebOrders/Details/5
        public async Task<ActionResult> Details(long? id)
        {
            // TODO: Validate that the order id is from the logged in user
            // TODO: Add a cancel button order to the view

            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            WebOrder webOrder = await db.WebOrders.FindAsync(id);
            /*if (webOrder == null)
            {
                return HttpNotFound();
            }*/

            // TODO: Remove this and add back DB usage
            webOrder = new WebOrder();
            return View(webOrder);
        }


        // POST: WebOrders/CancelOrder/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> CancelOrder(Guid? orderId)
        {
            // TODO: Confirm the order is for the current user
            WebOrder orderToCancel;

            if (orderId != null)
            {
                orderToCancel = await db.WebOrders.FindAsync(orderId);
                db.WebOrders.Remove(orderToCancel);

                await db.SaveChangesAsync();
            }

            return RedirectToAction("Index");
        }

        public async Task<ActionResult> UnprocessedOrders()
        {
            var unprocessedOrders =
                await db.WebOrders.Where(wo => wo.OrderStatus == OrderStatus.PendingProcessing).ToListAsync();

            return View(unprocessedOrders);
        }

        public async Task<ActionResult> UnprocessedOrderDetails(long? id)
        {
            WebOrder order = await db.WebOrders.FindAsync(id);

            return View(order);
        }
    }
}
