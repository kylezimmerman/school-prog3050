using Microsoft.AspNet.Identity;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using Veil.DataAccess.Interfaces;
using Veil.DataModels.Models;
using Veil.DataModels.Models.Identity;
using Veil.Services;
using Veil.Helpers;

namespace Veil.Controllers
{
    public class WishlistController : Controller
    {
        private IVeilDataAccess db;

        private readonly VeilUserManager userManager;

        public WishlistController(IVeilDataAccess veilDataAccess, VeilUserManager userManager)
        {
            db = veilDataAccess;
            this.userManager = userManager;
        }

        // GET: Wishlist
        public async Task<ActionResult> Index(Guid? wishlistOwnerId)
        {
            Member wishListMember;

            if (!User.Identity.IsAuthenticated)
            {
                if (wishlistOwnerId != null)
                {
                    wishListMember = await db.Members.FindAsync(wishlistOwnerId);
                    if (wishListMember != null &&
                        wishListMember.WishListVisibility == WishListVisibility.Public)
                    {
                        return View(wishListMember);
                    }
                }
                this.AddAlert(AlertType.Error, "Wishlist not found.");
                if (Request.UrlReferrer != null)
                {
                    return Redirect(Request.UrlReferrer.ToString());
                }
                return RedirectToAction("Index", "Home");
            }

            User user = await userManager.FindByIdAsync(Guid.Parse(User.Identity.GetUserId()));

            if (wishlistOwnerId == null)
            {
                // If a wishlistOwnerId was not given, the user is viewing their own wishlist
                wishListMember = user.Member;
            }
            else
            {
                wishListMember = await db.Members.FindAsync(wishlistOwnerId);
            }
            
            if (wishListMember == null)
            {
                this.AddAlert(AlertType.Error, "Wishlist not found.");
                return RedirectToAction("Index", "FriendList");
            }

            if (wishListMember.WishListVisibility == WishListVisibility.Private &&
                wishListMember.UserId != user.Id)
            {
                this.AddAlert(AlertType.Error, wishListMember.UserAccount.UserName + "'s wishlist is private.");
                if (Request.UrlReferrer != null)
                {
                    return Redirect(Request.UrlReferrer.ToString());
                }
                return RedirectToAction("Index", "Home");
            }
            else if (wishListMember.WishListVisibility == WishListVisibility.FriendsOnly &&
                (wishListMember.UserId != user.Id &&
                !wishListMember.ConfirmedFriends.Contains(user.Member)))
            {
                this.AddAlert(AlertType.Error, wishListMember.UserAccount.UserName + "'s wishlist is only available to their friends.");
                return RedirectToAction("Index", "FriendList");
            }

            return View(wishListMember);
        }

        public async Task<ActionResult> Add(Guid? itemId)
        {
            // TODO: Make this work for future Products that are not GameProducts
            Product newItem = await db.GameProducts.FindAsync(itemId) ?? new PhysicalGameProduct();
            User user = await userManager.FindByIdAsync(new Guid(User.Identity.GetUserId()));
            
            if (newItem == null)
            {
                this.AddAlert(AlertType.Error, "Error adding product to wishlist.");
                return Redirect(Request.UrlReferrer.ToString());
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

        public async Task<ActionResult> Remove(Guid? itemId)
        {
            Product toRemove = await db.GameProducts.FindAsync(itemId) ?? new PhysicalGameProduct();
            User user = await userManager.FindByIdAsync(new Guid(User.Identity.GetUserId()));

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
