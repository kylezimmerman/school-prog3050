using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using Veil.DataAccess;
using Veil.DataModels.Models;
using Veil.Models;

namespace Veil.Controllers
{
    public class CheckoutController : Controller
    {
        private VeilDataContext db = new VeilDataContext();

        // GET: Checkout/ShippingInfo
        [HttpGet]
        public async Task<ActionResult> ShippingInfo()
        {
            Member currentMember = new Member();

            IEnumerable<MemberAddress> addresses =
                db.MemberAddresses.Where(ma => ma.MemberId == currentMember.UserId);

            return View(addresses);
        }

        // POST: Checkout/ShippingInfo
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ShippingInfo(MemberAddress address)
        {
            // TODO: Create WebOrder for this cart
            // TODO: Add address to the WebOrder
            // TODO: Add incomplete WebOrder or CheckoutViewModel to session

            return RedirectToAction("BillingInfo");
        }

        // GET: Checkout/BillingInfo
        [HttpGet]
        public async Task<ActionResult> BillingInfo()
        {
            Member currentMember = new Member();

            IEnumerable<MemberCreditCard> billingInfos = currentMember.CreditCards;

            return View(billingInfos);
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

            return RedirectToAction("Index", "Home");
        }
    }
}