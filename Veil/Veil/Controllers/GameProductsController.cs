/* GameProductsController.cs
 * Purpose: Controller for actions related to the GameProduct model and derived models
 * 
 * Revision History:
 *      Kyle Zimmerman, 2015.11.9: Created
 */ 

using System;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.SqlClient;
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
    /// <summary>
    ///     Controller for actions related to <see cref="GameProduct"/>
    /// </summary>
    public class GameProductsController : BaseController
    {
        private readonly IVeilDataAccess db;
        private readonly IGuidUserIdGetter idGetter;

        /// <summary>
        ///     Instantiates a new instance of GameProductsController with the provided arguments
        /// </summary>
        /// <param name="veilDataAccess">
        ///     The <see cref="IVeilDataAccess"/> to use for database access
        /// </param>
        /// <param name="idGetter">
        ///     The <see cref="IGuidUserIdGetter"/> to use for getting the current user's Id
        /// </param>
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

        /// <summary>
        ///     Displays a delete confirmation page for the identified game SKU
        /// </summary>
        /// <param name="id">
        ///     The Id of the game SKU to delete
        /// </param>
        /// <returns>
        ///     The delete confirmation page if a match is found
        ///     404 Not Found page if a match is not found
        /// </returns>
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

        /// <summary>
        ///     Deletes the identified game including all of its empty ProductLocationInventories
        /// </summary>
        /// <param name="id">
        ///     The Id of the game SKU to delete
        /// </param>
        /// <returns>
        ///     Redirection to Games/Index if successful
        ///     Redirection to Delete to redisplay the confirmation page if unsuccessful
        ///     404 Not Found if no game matches the Id
        /// </returns>
        [Authorize(Roles = VeilRoles.Authorize.Admin_Employee)]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmed(Guid id)
        {
            GameProduct gameProduct = await db.GameProducts.
                Include(gp => gp.Game).
                Include(gp => gp.Platform).
                SingleOrDefaultAsync(gp => gp.Id == id);

            if (gameProduct == null)
            {
                throw new HttpException(NotFound, "Game SKU");
            }

            string gameName = gameProduct.Game.Name;
            string platform = gameProduct.Platform.PlatformName;

            try
            {
                db.ProductLocationInventories.RemoveRange(
                    await db.ProductLocationInventories.Where(
                        pli =>
                            pli.ProductId == id &&
                                pli.NewOnHand == 0 &&
                                pli.UsedOnHand == 0 &&
                                pli.NewOnOrder == 0).ToListAsync()
                    );

                db.GameProducts.Remove(gameProduct);

                await db.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                // Get the exception which states if a foreign key constraint was violated
                SqlException innermostException = ex.GetBaseException() as SqlException;

                bool errorWasConstraintViolation = false;

                if (innermostException != null)
                {
                    errorWasConstraintViolation =
                        innermostException.Number == (int) SqlErrorNumbers.ConstraintViolation;
                }

                if (errorWasConstraintViolation)
                {
                    this.AddAlert(
                        AlertType.Error,
                        "Other portions of our system depend on this Game SKU's data." +
                            " Consider marking it as not for sale instead.");
                }
                else
                {
                    this.AddAlert(
                        AlertType.Error, $"There was an error deleting {gameName} for {platform}.");
                }

                return RedirectToAction("Delete", new { id = id });
            }

            this.AddAlert(AlertType.Success, $"{gameName} for {platform} was deleted succesfully.");

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
        [Authorize(Roles = VeilRoles.Authorize.Admin_Employee)]
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
        [Authorize(Roles = VeilRoles.Authorize.Admin_Employee)]
        public async Task<ActionResult> CreatePhysicalSKU(
            Guid id,
            [Bind(
                Exclude =
                    nameof(PhysicalGameProduct.NewInventory) + "," +
                        nameof(PhysicalGameProduct.UsedInventory))] PhysicalGameProduct gameProduct)
        {
            if (!await db.Games.AnyAsync(g => g.Id == id))
            {
                throw new HttpException(NotFound, nameof(Game));
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
        [Authorize(Roles = VeilRoles.Authorize.Admin_Employee)]
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
        [Authorize(Roles = VeilRoles.Authorize.Admin_Employee)]
        public async Task<ActionResult> EditPhysicalSKU(Guid? id, [Bind] PhysicalGameProduct gameProduct)
        {
            if (id == null)
            {
                throw new HttpException(NotFound, "Physical Game Product");
            }

            if (ModelState.IsValid)
            {
                db.MarkAsModified(gameProduct);
                await db.SaveChangesAsync();

                this.AddAlert(AlertType.Success, "Successfully edited the Physical Game Product");
                return RedirectToAction("Details", "Games", new { id = gameProduct.GameId });
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
        [Authorize(Roles = VeilRoles.Authorize.Admin_Employee)]
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
        [Authorize(Roles = VeilRoles.Authorize.Admin_Employee)]
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
        [Authorize(Roles = VeilRoles.Authorize.Admin_Employee)]
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
        [Authorize(Roles = VeilRoles.Authorize.Admin_Employee)]
        public async Task<ActionResult> EditDownloadSKU(Guid? id, DownloadGameProduct gameProduct)
        {
            if (id == null)
            {
                throw new HttpException(NotFound, "Download Game Product");
            }

            if (ModelState.IsValid)
            {
                db.MarkAsModified(gameProduct);
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
            gameProduct.Game = await db.Games.FindAsync(gameId);
            db.GameProducts.Add(gameProduct);

            await db.SaveChangesAsync();

            // TODO: This is new, untested code
            if (gameProduct is PhysicalGameProduct)
            {
                ProductLocationInventory onlineInventory = new ProductLocationInventory
                {
                    ProductId = gameProduct.Id,
                    LocationId = await db.Locations.
                        Where(l => l.SiteName == Location.ONLINE_WAREHOUSE_NAME).
                        Select(l => l.Id).
                        FirstOrDefaultAsync(),
                    NewOnHand = 0,
                    UsedOnHand = 0,
                    NewOnOrder = 0
                };

                db.ProductLocationInventories.Add(onlineInventory);

                await db.SaveChangesAsync();
            } // TODO: End new untested code

            this.AddAlert(AlertType.Success, "Successfully added a new SKU.");

            return RedirectToAction("Details", "Games", new { id = gameId });
        }
    }
}