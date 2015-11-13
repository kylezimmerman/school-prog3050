using System;
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
    [Authorize(Roles = VeilRoles.MEMBER_ROLE)]
    public class CartController : BaseController
    {
        private readonly IVeilDataAccess db;
        private readonly IGuidUserIdGetter idGetter;

        public const string CART_QTY_KEY = "Cart.Quantity";
        
        public CartController(IVeilDataAccess veilDataAccess, IGuidUserIdGetter idGetter)
        {
            db = veilDataAccess;
            this.idGetter = idGetter;
        }

        // GET: Cart
        public async Task<ActionResult> Index()
        {
            // TODO: Remove this and actually implement it
            //Member currentMember = new Member();
            //currentMember.Cart = new Cart();

            //return View(currentMember.Cart);
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> AddItem(Guid? productId, bool isNew = true)
        {
            // TODO: Actually implement this
            //started
            var membersId = idGetter.GetUserId(User.Identity);
            Cart memberCart = await db.Carts.FindAsync(membersId);

            if (productId == null)
            {
                throw new HttpException(NotFound, nameof(Game));
            }

            GameProduct gameProduct = await db.GameProducts.FindAsync(productId);

            if (gameProduct == null)
            {
                throw new HttpException(NotFound, nameof(Game));
            }
            if (memberCart == null)
            {
                throw new HttpException(NotFound, nameof(Member));
            }

            CartItem cartItem = new CartItem()
            {
                MemberId = membersId,
                IsNew = isNew,
                ProductId = gameProduct.Id,
                Quantity = 1
            };


            try
            {
                memberCart.Items.Add(cartItem);
                await db.SaveChangesAsync();
            }
            catch (Exception)
            {

                throw;
            }


            // TODO: Update Cart Quantity in Session
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> RemoveItem(Guid? productId)
        {
            // TODO: Actually implement this
            // TODO: Update Cart Quantity in Session
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> UpdateQuantity(Guid? productId, int? quantity)
        {
            // TODO: Actually implement this
            return RedirectToAction("Index");
        }



        /// <summary>
        ///     Stores the number of items in the current member's cart in the Session
        /// </summary>
        [ChildActionOnly]
        public void SetSessionCartQty()
        {
            Guid currentUserId = idGetter.GetUserId(User.Identity);
            Member currentMember = db.Members.Find(currentUserId);

            Session[CART_QTY_KEY] = currentMember.Cart.Items.Count;
        }
    }
}
