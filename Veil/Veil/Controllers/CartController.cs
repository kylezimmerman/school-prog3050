using System;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Linq;
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
        /// <summary>
        ///     Displays a list of items in the current member's cart
        /// </summary>
        /// <returns>
        ///     Index view with the current member's cart items
        /// </returns>
        public async Task<ActionResult> Index()
        {
            return View(await db.Carts.FindAsync(idGetter.GetUserId(User.Identity)));
        }

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

        public async Task<ActionResult> RemoveItem(Guid? productId)
        {
            // TODO: Actually implement this
            // TODO: Update Cart Quantity in Session
            return RedirectToAction("Index");
        }

        // POST: Cart/UpdateQuantity
        /// <summary>
        ///     Updates the quantity of an item
        /// </summary>
        /// <param name="productId">
        ///     The Id of the product to change the quantity of
        /// </param>
        /// <param name="isNew">
        ///     True if the item is new
        /// </param>
        /// <param name="quantity">
        ///     The new quantity for the item
        /// </param>
        /// <returns>
        ///     Index view with the current member's cart items updated with the new quantity for the specified line
        /// </returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> UpdateQuantity(Guid? productId, bool? isNew, int? quantity)
        {
            if (productId == null || isNew == null || quantity == null)
            {
                return RedirectToAction("Index");
            }

            Cart currentMemberCart = await db.Carts.FindAsync(idGetter.GetUserId(User.Identity));
            CartItem item = currentMemberCart.Items.FirstOrDefault(i => i.ProductId == productId && i.IsNew == isNew);

            if (item == null)
            {
                throw new HttpException(NotFound, nameof(CartItem));
            }

            item.Quantity = quantity.Value;
            await db.SaveChangesAsync();

            this.AddAlert(AlertType.Success, item.Product.Name + " quantity set to " + quantity);
            return RedirectToAction("Index");
        }

        /// <summary>
        ///     Stores the number of items in the current member's cart in the Session.
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
