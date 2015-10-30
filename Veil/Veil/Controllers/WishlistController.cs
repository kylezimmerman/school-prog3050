using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Threading.Tasks;
using System.Web.Mvc;
using Veil.DataAccess.Interfaces;
using Veil.DataModels.Models;

namespace Veil.Controllers
{
    public class WishlistController : Controller
    {
        private IVeilDataAccess db;

        public WishlistController(IVeilDataAccess veilDataAccess)
        {
            db = veilDataAccess;
        }

        // GET: Wishlist
        public async Task<ActionResult> Index(Guid? userId)
        {
            Member wishListMember;

            if (userId == null)
            {
                // TODO: Get current member from user manager
                wishListMember = await db.Members.FirstOrDefaultAsync();

                // TODO: Remove this for actual implementation
                if (wishListMember == null)
                {
                    wishListMember = new Member { Wishlist = new List<Product>() };
                }
            }
            else
            {
                wishListMember = await db.Members.FindAsync(userId);
            }

            return View(wishListMember.Wishlist);
        }

        public async Task<ActionResult> Add(Guid? itemId)
        {
            // TODO: Add the specified item from the signed in user's wish list

            return RedirectToAction("Index");
        }

        public async Task<ActionResult> Remove(Guid? itemId)
        {
            // TODO: Remove the specified item from the signed in user's wish list

            return RedirectToAction("Index");
        }
    }
}
