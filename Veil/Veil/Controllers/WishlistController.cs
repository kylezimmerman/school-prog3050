/* WishlistController.cs
 * Purpose: Controller for viewing and managing a wishlist of products
 * 
 * Revision History:
 *      Drew Matheson, 2015.10.13: Created
 */ 

using System;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using Veil.DataAccess.Interfaces;
using Veil.DataModels.Models;
using Veil.Helpers;
using Veil.Models;
using System.Web;
using Veil.DataModels;

namespace Veil.Controllers
{
    /// <summary>
    ///     Controller for actions related to viewing and managing a wishlist of products
    /// </summary>
    public class WishlistController : BaseController
    {
        private readonly IVeilDataAccess db;
        private readonly IGuidUserIdGetter idGetter;

        /// <summary>
        ///     Instantiates a new instance of WishlistController with the provided arguments
        /// </summary>
        /// <param name="veilDataAccess">
        ///     The <see cref="IVeilDataAccess"/> to use for database access
        /// </param>
        /// <param name="idGetter">
        ///     The <see cref="IGuidUserIdGetter"/> to use for getting the current user's Id
        /// </param>
        public WishlistController(IVeilDataAccess veilDataAccess, IGuidUserIdGetter idGetter)
        {
            db = veilDataAccess;
            this.idGetter = idGetter;
        }

        // GET: Wishlist
        /// <summary>
        ///     Displays the wishlist of the user indicated
        ///     If no Username is given the current user's wishlist is shown
        /// </summary>
        /// <param name="username">
        ///     The Username of the owner of the wishlist to be displayed. Set to current user if null.
        /// </param>
        /// <returns>
        ///     Index view for the Wishlist matching wishlistOwnerId
        ///     Index view for the current member's Wishlist if wishlistOwnerId is null
        ///     404 Not Found view if the id does not match a Member
        /// </returns>
        public async Task<ActionResult> Index(string username)
        {
            Member wishlistOwner;

            if (!User.Identity.IsAuthenticated)
            {
                if (username != null)
                {
                    // Even anonymous users can see public wishlists
                    wishlistOwner =
                        await db.Members.FirstOrDefaultAsync(m => m.UserAccount.UserName == username);

                    if (wishlistOwner != null &&
                        wishlistOwner.WishListVisibility == WishListVisibility.Public)
                    {
                        return View(wishlistOwner);
                    }
                }
                throw new HttpException(NotFound, "Wishlist");
            }

            Member currentMember = await db.Members.FindAsync(idGetter.GetUserId(User.Identity));

            if (username == null)
            {
                // If a wishlistOwnerId was not given, the user is viewing their own wishlist
                wishlistOwner = currentMember;
            }
            else
            {
                wishlistOwner =
                    await db.Members.FirstOrDefaultAsync(m => m.UserAccount.UserName == username);
            }

            if (wishlistOwner == null)
            {
                throw new HttpException(NotFound, "Wishlist");
            }

            if (wishlistOwner.WishListVisibility == WishListVisibility.Private &&
                wishlistOwner.UserId != currentMember.UserId)
            {
                throw new HttpException(NotFound, "Wishlist");
            }

            if (wishlistOwner.WishListVisibility == WishListVisibility.FriendsOnly &&
                (wishlistOwner.UserId != currentMember.UserId &&
                    !wishlistOwner.ConfirmedFriends.Contains(currentMember)))
            {
                this.AddAlert(
                    AlertType.Error,
                    wishlistOwner.UserAccount.UserName + "'s wishlist is only available to their friends.");
                return RedirectToAction("Index", "FriendList");
            }

            return View(wishlistOwner);
        }

        /// <summary>
        ///     Gets a partial view for a single PhysicalGameProduct on the wishlist
        /// </summary>
        /// <param name="gameProduct">
        ///     The gameProduct for this line of the list
        /// </param>
        /// <param name="wishlistOwnerId">
        ///     The ID of the owner of the wishlist
        /// </param>
        /// <returns>
        ///     Partial view for the provided gameProduct
        /// </returns>
        [ChildActionOnly]
        public ActionResult RenderPhysicalGameProduct(
            PhysicalGameProduct gameProduct, Guid wishlistOwnerId)
        {
            var model = new WishlistPhysicalGameProductViewModel
            {
                GameProduct = gameProduct
            };

            Member currentMember = db.Members.Find(idGetter.GetUserId(User.Identity));

            if (currentMember != null)
            {
                model.NewIsInCart =
                    currentMember.Cart.Items.Any(i => i.ProductId == gameProduct.Id && i.IsNew);
                model.UsedIsInCart =
                    currentMember.Cart.Items.Any(i => i.ProductId == gameProduct.Id && !i.IsNew);
                model.ProductIsOnWishlist = currentMember.Wishlist.Any(p => p.Id == gameProduct.Id);
                model.MemberIsCurrentUser = currentMember.UserId == wishlistOwnerId;
            }

            return PartialView("_PhysicalGameProductPartial", model);
        }

        /// <summary>
        ///     Adds an item to the current member's wishlist.
        /// </summary>
        /// <param name="itemId">
        ///     The ID of the product to be added.
        /// </param>
        /// <returns>
        ///     Index view for the current member's Wishlist
        ///     404 Not Found view if itemId does not match a Product
        /// </returns>
        [Authorize(Roles = VeilRoles.MEMBER_ROLE)]
        public async Task<ActionResult> Add(Guid? itemId)
        {
            Product newItem = await db.Products.FindAsync(itemId);

            if (newItem == null)
            {
                throw new HttpException(NotFound, nameof(Product));
            }

            Member currentMember = await db.Members.FindAsync(idGetter.GetUserId(User.Identity));

            if (currentMember.Wishlist.Contains(newItem))
            {
                this.AddAlert(AlertType.Info, newItem.Name + " is already on your wishlist.");
            }
            else
            {
                currentMember.Wishlist.Add(newItem);
                await db.SaveChangesAsync();

                this.AddAlert(AlertType.Success, newItem.Name + " was added to your wishlist.");
            }

            return RedirectToAction("Index");
        }

        /// <summary>
        ///     Removes an item from the current member's wishlist.
        /// </summary>
        /// <param name="itemId">
        ///     The ID of the product to be removed.
        /// </param>
        /// <returns>
        ///     Index view for the current member's Wishlist
        ///     404 Not Found view if itemId does not match a Product
        /// </returns>
        [Authorize(Roles = VeilRoles.MEMBER_ROLE)]
        public async Task<ActionResult> Remove(Guid? itemId)
        {
            Product toRemove = await db.Products.FindAsync(itemId);

            if (toRemove == null)
            {
                throw new HttpException(NotFound, nameof(Product));
            }

            Member currentMember = await db.Members.FindAsync(idGetter.GetUserId(User.Identity));

            if (!currentMember.Wishlist.Contains(toRemove))
            {
                this.AddAlert(AlertType.Error, toRemove.Name + " is not on your wishlist.");
            }
            else
            {
                currentMember.Wishlist.Remove(toRemove);
                await db.SaveChangesAsync();

                this.AddAlert(AlertType.Success, toRemove.Name + " was removed from your wishlist.");
            }
            
            return RedirectToAction("Index");
        }
    }
}