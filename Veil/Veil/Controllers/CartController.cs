using System;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Linq;
using Microsoft.Ajax.Utilities;
using Veil.DataAccess.Interfaces;
using Veil.DataModels;
using Veil.DataModels.Models;
using Veil.Extensions;
using Veil.Helpers;
using Member = Veil.DataModels.Models.Member;

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
            if (productId == null)
            {
                throw new HttpException(NotFound, nameof(Game));
            }

            var membersId = idGetter.GetUserId(User.Identity);
            Cart memberCart = await db.Carts.FindAsync(membersId);
            GameProduct gameProduct = await db.GameProducts.Include(db => db.Game).Include(db => db.Platform).FirstOrDefaultAsync(x => x.Id == productId);
            string name = gameProduct.Game.Name;
            string platform = gameProduct.Platform.PlatformName;
            Guid gameId = gameProduct.GameId;

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
                this.AddAlert(AlertType.Success, platform + ": " + name + " was succesfully added to your your cart");
                SetSessionCartQty();
            }
            catch (DbUpdateException)
            {
                this.AddAlert(AlertType.Error, "An error occured while adding "+ platform + ": " + name + " to your cart");
            }

            //do we really want this to redirect the user
            return RedirectToAction("Details", "Games", new { id = gameId });
        }

        public async Task<ActionResult> RemoveItem(Guid? productId)
        { 
            if (productId == null)
            {
                throw new HttpException(NotFound, "OOOOH NO");
            }

            Cart memberCart = await db.Carts.FindAsync(idGetter.GetUserId(User.Identity));
            if (memberCart == null)
            {
                throw new HttpException(NotFound, "Dear god");
            }

            CartItem cartItem = memberCart.Items.FirstOrDefault(x => x.ProductId == productId);
            GameProduct gameProduct = await db.GameProducts.Include(db => db.Game).Include(db => db.Platform).FirstOrDefaultAsync(x => x.Id == cartItem.ProductId);
            string name = gameProduct.Game.Name;
            string platform = gameProduct.Platform.PlatformName;
            Guid gameId = gameProduct.GameId;
            try
            {
                memberCart.Items.Remove(cartItem);
                await db.SaveChangesAsync();
                this.AddAlert(AlertType.Success, platform + ": " + name + " was succesfully removed for your cart");
                SetSessionCartQty();
            }
            catch (DbUpdateException)
            {
                this.AddAlert(AlertType.Error, "An error occured while removing " + platform + ": " + name + " from your cart");
            }

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
            return View("Index", currentMemberCart);
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
