using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Veil.DataAccess.Interfaces;
using Veil.DataModels;
using Veil.DataModels.Models;
using Veil.Extensions;
using Veil.Helpers;

namespace Veil.Controllers
{
    [Authorize]
    public class WebOrdersController : BaseController
    {
        private readonly IVeilDataAccess db;
        private readonly IGuidUserIdGetter idGetter;

        public WebOrdersController(IVeilDataAccess veilDataAccess, IGuidUserIdGetter idGetter)
        {
            db = veilDataAccess;
            this.idGetter = idGetter;
        }

        // GET: WebOrders
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
    }
}
