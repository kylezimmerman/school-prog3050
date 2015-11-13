using System;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Veil.DataAccess.Interfaces;
using Veil.DataModels;
using Veil.DataModels.Models;
using Veil.Helpers;
using Veil.Models;

namespace Veil.Controllers
{
    public class GameProductsController : BaseController
    {
        private readonly IVeilDataAccess db;
        private IGuidUserIdGetter idGetter;

        public GameProductsController(IVeilDataAccess veilDataAccess, IGuidUserIdGetter idGetter)
        {
            db = veilDataAccess;
            this.idGetter = idGetter;
        }

        /// <summary>
        ///     Renders the Game SKU partial for a physical game product
        /// </summary>
        /// <param name="gameProduct">
        ///     The physical game sku to render.
        /// </param>
        /// <returns>
        ///     Partial view containing the information specific to PhysicalGameProducts
        /// </returns>
        [ChildActionOnly]
        public PartialViewResult RenderPhysicalGameProductPartial(PhysicalGameProduct gameProduct)
        {
            PhysicalGameProductViewModel model = new PhysicalGameProductViewModel
            {
                GameProduct = gameProduct
            };

            Member currentMember = db.Members.Find(idGetter.GetUserId(User.Identity));

            if (currentMember != null)
            {
                model.NewIsInCart = currentMember.Cart.Items.
                    Any(i => i.ProductId == gameProduct.Id && i.IsNew);

                model.UsedIsInCart = currentMember.Cart.Items.
                    Any(i => i.ProductId == gameProduct.Id && !i.IsNew);

                model.ProductIsOnWishlist = currentMember.Wishlist.
                    Any(p => p.Id == gameProduct.Id);
            }

            return PartialView("_PhysicalGameProductPartial", model);
        }

        [Authorize(Roles = VeilRoles.Authorize.Admin_Employee)]
        public async Task<ActionResult> Delete(Guid? id)
        {
            if (id == null)
            {
                throw new HttpException(NotFound, "There was an error");
            }

            GameProduct gameProduct = await db.GameProducts.FindAsync(id);

            if (gameProduct == null)
            {
                //replace this when it is finished
                throw new HttpException(NotFound, "There was an error");
            }

            return View(gameProduct);
        }

        [Authorize(Roles = VeilRoles.ADMIN_ROLE + "," + VeilRoles.EMPLOYEE_ROLE)]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmed(Guid id = default(Guid))
        {
            if (id == Guid.Empty)
            {
                throw new HttpException(NotFound, "There was an error");
            }

            GameProduct gameProduct = await db.GameProducts.FindAsync(id);

            if (gameProduct != null)
            {
                try
                {
                    db.GameProducts.Remove(gameProduct);
                    await db.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    this.AddAlert(AlertType.Error, "There was an error deleting " + gameProduct.Platform + ": " + gameProduct.Name);
                    return View(gameProduct);
                }
            }
            else
            {
                // TODO: Actually give this a message. Cmon!
                throw new HttpException(NotFound, "some message");
            }

            return RedirectToAction("Index", "Games");
        }

        /// <summary>
        /// A view to create a new PhysicalGameProduct
        /// </summary>
        /// <param name="id">The ID of the game to add this product to</param>
        /// <returns>
        ///     Redirects to the Game Details page if successful
        ///     Redirects to the Game List page if the id does not match an existing Game.
        /// </returns>
        public async Task<ActionResult> CreatePhysicalSKU(Guid id)
        {
            if (!await db.Games.AnyAsync(g => g.Id == id))
            {
                throw new HttpException(NotFound, "Game");
            }

            SetupGameProductSelectLists();

            return View();
        }

        /// <summary>
        /// Creates an saves a new PhysicalGameProduct to the database.
        /// </summary>
        /// <param name="id">The ID of the game to add this product to</param>
        /// <param name="gameProduct">The PhysicalGameProduct to add to the game</param>
        /// <returns>
        ///     Redirects to the Game Details page if successful
        ///     A view of the create screen with validation errors if there were any
        ///     Redirects to the Game List if the id doesn't match an existing Game.
        /// </returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> CreatePhysicalSKU(Guid id, 
            [Bind(Exclude = nameof(PhysicalGameProduct.NewInventory) + "," + nameof(PhysicalGameProduct.UsedInventory))] PhysicalGameProduct gameProduct)
        {
            if (!await db.Games.AnyAsync(g => g.Id == id))
            {
                throw new HttpException(NotFound, "Game");
            }

            if (ModelState.IsValid)
            {
                var internalSku = db.GetNextPhysicalGameProductSku();

                gameProduct.InteralUsedSKU = $"1{internalSku}";
                gameProduct.InternalNewSKU = $"0{internalSku}";

                return await SaveGameProduct(id, gameProduct);
            }

            SetupGameProductSelectLists();

            return View(gameProduct);
        }

        /// <summary>
        /// Displays existing information for a Physical Game Product and allows the user to edit it.
        /// </summary>
        /// <param name="id">The ID of the Physical Game Product to edit</param>
        /// <returns>
        ///     Edit view if the Id is for a physical game product
        ///     404 Not Found view if the Id couldn't be matched to a game product
        /// </returns>
        public async Task<ActionResult> EditPhysicalSKU(Guid? id)
        {
            if (id == null)
            {
                throw new HttpException(NotFound, "Physical Game Product");
            }

            var physicalGameProduct = await db.PhysicalGameProducts.FirstOrDefaultAsync(p => p.Id == id);

            if (physicalGameProduct == null)
            {
                throw new HttpException(NotFound, "Physical Game Product");
            }

            SetupGameProductSelectLists();

            return View(physicalGameProduct);
        }

        /// <summary>
        /// Saves the changes made to a Physical Game Product 
        /// </summary>
        /// <param name="id">The ID of the Physical Game Product to edit</param>
        /// <param name="gameProduct">The PhysicalGameProduct with the edited values.</param>
        /// <returns>
        ///     Redirects to the Game Details page if the Id is for a physical game product and the edited information is valid
        ///     A view of the Edit Physical Game Product page if the Id is for a physical game broduct but has validation errors
        ///     404 Not Found view if the Id couldn't be matched to a game product
        /// </returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> EditPhysicalSKU(Guid? id, [Bind]PhysicalGameProduct gameProduct)
        {
            if (id == null)
            {
                throw new HttpException(NotFound, "Physical Game Product");
            }

            if (ModelState.IsValid)
            {
                await db.SaveChangesAsync();

                this.AddAlert(AlertType.Success, "Successfully edited the Physical Game Product");
                return RedirectToAction("Details", "Games", new {id = gameProduct.GameId });
            }

            SetupGameProductSelectLists();

            return View(gameProduct);
        }

        /// <summary>
        /// Displays a view for the user to create a new Download Game Product
        /// </summary>
        /// <param name="id">The ID of the Game that this is a product for</param>
        /// <returns>
        ///     A View to create the new downloadable product for if the id is a valid game
        ///     Redirects to the Game List page if the game id was invalid
        /// </returns>
        public async Task<ActionResult> CreateDownloadSKU(Guid id)
        {
            if (!await db.Games.AnyAsync(g => g.Id == id))
            {
                throw new HttpException(NotFound, "Game");
            }

            SetupGameProductSelectLists();

            return View();
        }

        /// <summary>
        /// Creates and saves a new Downloadable Game Product to the database.
        /// </summary>
        /// <param name="id">The ID of the Game that this is a product for</param>
        /// <param name="gameProduct">The GameProduct to be created and saved</param>
        /// <returns>
        ///     Redirects to the Game Page with an alert if the game id and game product are valid
        ///     A view to correct any errors if the game id is valid but the game product is not
        ///     Redirects to the Game List page with an alert if the game id is invalid
        /// </returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> CreateDownloadSKU(Guid id, [Bind] DownloadGameProduct gameProduct)
        {
            if (!await db.Games.AnyAsync(g => g.Id == id))
            {
                throw new HttpException(NotFound, "Game");
            }

            if (ModelState.IsValid)
            {
                return await SaveGameProduct(id, gameProduct);
            }

            SetupGameProductSelectLists();

            return View(gameProduct);
        }

        /// <summary>
        /// Displays a view to edit download game products
        /// </summary>
        /// <param name="id">The ID of the download game product to edit</param>
        /// <returns>
        ///     A view to edit the existing Download Game Product
        ///     404 Not Found if the id does not map to a valid Download Game Product
        /// </returns>
        public async Task<ActionResult> EditDownloadSKU(Guid? id)
        {
            if (id == null)
            {
                throw new HttpException(NotFound, "Download Game Product");
            }

            var physicalGameProduct = await db.DownloadGameProducts.FirstOrDefaultAsync(p => p.Id == id);

            if (physicalGameProduct == null)
            {
                throw new HttpException(NotFound, "Download Game Product");
            }

            SetupGameProductSelectLists();

            return View(physicalGameProduct);
        }

        /// <summary>
        /// Saves changes to an existing DownloadGameProduct
        /// </summary>
        /// <param name="id">The ID of the DownloadGameProduct to edit</param>
        /// <param name="gameProduct">The DownloadGameProduct with edited values to save.</param>
        /// <returns>
        ///     Redirects to the Game's details page if successful
        ///     The edit view to fix any validation errors if the provided DownloadGameProduct is invalid
        ///     404 Not Found if the id was not provided
        /// </returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> EditDownloadSKU(Guid? id, DownloadGameProduct gameProduct)
        {
            if (id == null)
            {
                throw new HttpException(NotFound, "Download Game Product");
            }

            if (ModelState.IsValid)
            {
                await db.SaveChangesAsync();

                this.AddAlert(AlertType.Success, "Successfully edited the Download Game Product");
                return RedirectToAction("Details", "Games", new { id = gameProduct.GameId });
            }

            SetupGameProductSelectLists();

            return View(gameProduct);
        }

        /// <summary>
        /// Sets up the required Select Lists in Viewbag so that dropdowns can work
        /// </summary>
        private void SetupGameProductSelectLists()
        {
            ViewBag.PlatformCode = new SelectList(db.Platforms, "PlatformCode", "PlatformName");
            ViewBag.DeveloperId = new SelectList(db.Companies, "Id", "Name");
            ViewBag.PublisherId = new SelectList(db.Companies, "Id", "Name");
        }

        /// <summary>
        /// Saves a new Game Product and attaches it to a game.
        /// </summary>
        /// <param name="gameId">The Id of the game this product belongs to</param>
        /// <param name="gameProduct">The new gameProduct to save</param>
        /// <returns>Redirects to the details page for the game.</returns>
        private async Task<ActionResult> SaveGameProduct(Guid gameId, GameProduct gameProduct)
        {
            gameProduct.Id = Guid.NewGuid();
            gameProduct.Game = await db.Games.FindAsync(gameId);
            db.GameProducts.Add(gameProduct);

            await db.SaveChangesAsync();

            this.AddAlert(AlertType.Success, "Successfully added a new SKU.");

            return RedirectToAction("Details", "Games", new { id = gameId });
        }
    }
}