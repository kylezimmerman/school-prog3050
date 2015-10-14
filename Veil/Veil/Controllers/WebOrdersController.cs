using System;
using System.Data.Entity;
using System.Threading.Tasks;
using System.Net;
using System.Web.Mvc;
using Veil.DataAccess;
using Veil.DataModels.Models;

namespace Veil.Controllers
{
    public class WebOrdersController : Controller
    {
        private VeilDataContext db = new VeilDataContext();

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

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
