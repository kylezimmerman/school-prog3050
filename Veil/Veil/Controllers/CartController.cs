﻿using System;
using System.Threading.Tasks;
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
            return View(await db.Carts.FindAsync(idGetter.GetUserId(User.Identity)));
        }

        public async Task<ActionResult> AddItem(Guid? productId, bool isNew = true)
        {
            // TODO: Actually implement this
            // TODO: Update Cart Quantity in Session
            return RedirectToAction("Index");
        }

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
            if (productId == null || quantity == null)
            {
                return RedirectToAction("Index");
            }

            Cart currentMemberCart = await db.Carts.FindAsync(idGetter.GetUserId(User.Identity));

            this.AddAlert(AlertType.Success, "Quantity set to " + quantity);
            return View("Index", currentMemberCart);
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
