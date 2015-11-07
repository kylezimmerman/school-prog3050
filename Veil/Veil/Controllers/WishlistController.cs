using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using Veil.DataAccess.Interfaces;
using Veil.DataModels.Models;
using Veil.DataModels.Models.Identity;
using Veil.Services;
using Veil.Helpers;
using Veil.Extensions;
using Veil.Models;
using System.Data.Entity;
using System.Web;
using System.Net;

namespace Veil.Controllers
{
    public class WishlistController : BaseController
    {
        private readonly IVeilDataAccess db;

        private readonly VeilUserManager userManager;

        public WishlistController(IVeilDataAccess veilDataAccess, VeilUserManager userManager)
        {
            db = veilDataAccess;
            this.userManager = userManager;
        }

        // GET: Wishlist
        /// <summary>
        /// Displays the wishlist of the user indicated
        /// If no ID is given the current user's wishlist is shown
        /// </summary>
        /// <param name="wishlistOwnerId">The ID of the owner of the wishlist to be displayed. Set to current user if null.</param>
        /// <returns></returns>
        public async Task<ActionResult> Index(Guid? wishlistOwnerId)
        {
            Member wishlistOwner;

            if (!User.Identity.IsAuthenticated)
            {
                if (wishlistOwnerId != null)
                {
                    // Even anonymous users can see public wishlists
                    wishlistOwner = await db.Members.FindAsync(wishlistOwnerId);

                    if (wishlistOwner != null &&
                        wishlistOwner.WishListVisibility == WishListVisibility.Public)
                    {
                        return View(wishlistOwner);
                    }
                }
                throw new HttpException((int)HttpStatusCode.NotFound, "Wishlist");
            }

            Guid currentUserId = IIdentityExtensions.GetUserId(User.Identity);
            Member currentMember = await db.Members.FindAsync(currentUserId);

            if (wishlistOwnerId == null)
            {
                // If a wishlistOwnerId was not given, the user is viewing their own wishlist
                wishlistOwner = currentMember;
            }
            else
            {
                wishlistOwner = await db.Members.FindAsync(wishlistOwnerId);
            }
            
            if (wishlistOwner == null)
            {
                throw new HttpException((int)HttpStatusCode.NotFound, "Wishlist");
            }

            if (wishlistOwner.WishListVisibility == WishListVisibility.Private &&
                wishlistOwner.UserId != currentMember.UserId)
            {
                throw new HttpException((int)HttpStatusCode.NotFound, "Wishlist");
            }
            else if (wishlistOwner.WishListVisibility == WishListVisibility.FriendsOnly &&
                (wishlistOwner.UserId != currentMember.UserId &&
                !wishlistOwner.ConfirmedFriends.Contains(currentMember)))
            {
                this.AddAlert(AlertType.Error, wishlistOwner.UserAccount.UserName + "'s wishlist is only available to their friends.");
                return RedirectToAction("Index", "FriendList");
            }

            return View(wishlistOwner);
        }

        [ChildActionOnly]
        public ActionResult RenderPhysicalGameProduct(PhysicalGameProduct gameProduct, Guid wishlistOwnerId)
        {
            var model = new WishlistPhysicalGameProductViewModel
            {
                GameProduct = gameProduct
            };

            Guid currentUserId = IIdentityExtensions.GetUserId(User.Identity);
            Member currentMember = db.Members.Find(currentUserId);

            if (currentMember != null)
            {
                model.NewIsInCart = currentMember.Cart.Items.Any(i => i.ProductId == gameProduct.Id && i.IsNew);
                model.UsedIsInCart = currentMember.Cart.Items.Any(i => i.ProductId == gameProduct.Id && !i.IsNew);
                model.ProductIsOnWishlist = currentMember.Wishlist.Contains(gameProduct);
                model.MemberIsCurrentUser = currentMember.UserId == wishlistOwnerId;
            }

            return PartialView("_PhysicalGameProductPartial", model);
        }

        /// <summary>
        /// Adds an item to the current user's wishlist.
        /// </summary>
        /// <param name="itemId">The ID of the product to be added.</param>
        /// <returns></returns>
        public async Task<ActionResult> Add(Guid? itemId)
        {
            // TODO: Make this work for future Products that are not GameProducts
            Product newItem = await db.GameProducts.FindAsync(itemId) ?? new PhysicalGameProduct();
            User user = await userManager.FindByIdAsync(IIdentityExtensions.GetUserId(User.Identity));

            if (newItem == null)
            {
                this.AddAlert(AlertType.Error, "Error adding product to wishlist.");
                if (Request.UrlReferrer != null)
                {
                    return Redirect(Request.UrlReferrer.ToString());
                }
                return View();
            }

            if (user.Member.Wishlist.Contains(newItem))
            {
                this.AddAlert(AlertType.Info, newItem.Name + " is already on your wishlist.");
                return RedirectToAction("Index");
            }

            user.Member.Wishlist.Add(newItem);
            await db.SaveChangesAsync();

            this.AddAlert(AlertType.Success, newItem.Name + " was added to your wishlist.");
            return RedirectToAction("Index");
        }

        /// <summary>
        /// Removes a product from the current user's wishlist.
        /// </summary>
        /// <param name="itemId">The ID of the product to be removed.</param>
        /// <returns></returns>
        public async Task<ActionResult> Remove(Guid? itemId)
        {
            Product toRemove = await db.GameProducts.FindAsync(itemId) ?? new PhysicalGameProduct();
            User user = await userManager.FindByIdAsync(IIdentityExtensions.GetUserId(User.Identity));

            if (toRemove == null)
            {
                this.AddAlert(AlertType.Error, "Error removing product from wishlist.");
                return RedirectToAction("Index");
            }

            if (!user.Member.Wishlist.Contains(toRemove))
            {
                this.AddAlert(AlertType.Error, toRemove.Name + " is not on your wishlist.");
                return RedirectToAction("Index");
            }

            user.Member.Wishlist.Remove(toRemove);
            await db.SaveChangesAsync();

            this.AddAlert(AlertType.Success, toRemove.Name + " was removed from your wishlist.");
            return RedirectToAction("Index");
        }
    }
}
