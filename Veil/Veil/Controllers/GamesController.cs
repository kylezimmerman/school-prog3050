/* GamesController.cs
 * Purpose: Controller for the primarily for the Games model
 *          Also contains actions for GameProduct and derived models
 * 
 * Revision History:
 *      Isaac West, 2015.10.13: Created
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
using System.Collections.Generic;
using Veil.Models;
using System.Transactions;
using System.Net;
using System.Web;
using LinqKit;
using Veil.Extensions;

namespace Veil.Controllers
{
    public class GamesController : BaseController
    {
        private const int GAMES_PER_PAGE = 10;

        protected readonly IVeilDataAccess db;

        public GamesController(IVeilDataAccess veilDataAccess)
        {
            db = veilDataAccess;
        }

        /// <summary>
        ///     Displays a paginated list of games
        /// </summary>
        /// <param name="page">
        ///     The page number being requested
        /// </param>
        /// <returns>
        ///     A paginated list of games
        /// </returns>
        public async Task<ActionResult> Index(int page = 1)
        {
            IQueryable<Game> games = db.Games.Include(g => g.Rating);

            if (!User.IsEmployeeOrAdmin())
            {
                games = games.Where(g => g.GameAvailabilityStatus != AvailabilityStatus.NotForSale);
            }

            games = games.OrderBy(g => g.Name);

            var gamesListViewModel = new GameListViewModel()
            {
                Games = await games.Skip((page - 1)*GAMES_PER_PAGE).Take(GAMES_PER_PAGE).ToListAsync(),
                CurrentPage = page,
                TotalPages = (int) Math.Ceiling(await games.CountAsync()/(float) GAMES_PER_PAGE)
            };

            return View(gamesListViewModel);
            }

        /// <summary>
        ///     Processes simple search game search. This is used by the navbar search
        /// </summary>
        /// <param name="keyword">
        ///     Fragment of a game title to filter by
        /// </param>
        /// <returns>
        ///     IQueryable of type 'Game' to Index view of Games controller.
        /// </returns>
        [HttpPost]
        public async Task<ActionResult> Search(string keyword = "")
        {
            keyword = keyword.Trim();

            IQueryable<Game> gamesFiltered = db.Games
                .Where(g => g.Name.Contains(keyword));

            if (!User.IsEmployeeOrAdmin())
            {
                gamesFiltered = gamesFiltered.Where(g => g.GameAvailabilityStatus != AvailabilityStatus.NotForSale);
            }

            ViewBag.SearchTerm = keyword;

            return View("Index", await gamesFiltered.ToListAsync());
        }

        /// <summary>
        ///     Processes advanced game searches and displays the results
        /// </summary>
        /// <param name="tags">
        ///     List of tag names to filter by
        /// </param>
        /// <param name="title">
        ///     Title search string to filter by
        /// </param>
        /// <param name="platform">
        ///     Platform Code for the platform to filter by
        /// </param>
        /// <returns>
        ///     Index view with the filtered results
        /// </returns>
        public async Task<ActionResult> AdvancedSearch(
            List<string> tags, string title = "", string platform = "", int page = 1)
        {
            title = title.Trim();
            platform = platform.Trim();
            tags = tags ?? new List<string>();

            for (int i = 0; i < tags.Count; i++)
            {
                string t = tags[i];
                t = t.Trim();
                tags[i] = t;
            }

            if (tags.Count == 0 && title == "" && platform == "")
            {
                SearchViewModel searchViewModel = new SearchViewModel();
                searchViewModel.Platforms = await db.Platforms.ToListAsync();
                searchViewModel.Tags = await db.Tags.Where(t => tags.Contains(t.Name)).ToListAsync();

                return View(searchViewModel);
            }

            // We are doing Or, so we need the first to be false
            var searchPredicate = PredicateBuilder.False<Game>();

            if (!string.IsNullOrWhiteSpace(title))
            {
                // Filter by title
                searchPredicate = searchPredicate.Or(g => g.Name.Contains(title));
            }

            if (tags.Count > 0)
            {
                // Filter by tags
                searchPredicate = searchPredicate.Or(g => g.Tags.Any(t => tags.Contains(t.Name)));
            }

            if (!string.IsNullOrWhiteSpace(platform))
            {
                // Filter by platform
                searchPredicate = searchPredicate.Or(g => g.GameSKUs.Any(gs => gs.PlatformCode == platform));
            }

            if (!User.IsEmployeeOrAdmin())
            {
                // Filter out any not for sale games
                // We are doing And, so we need the first to be true
                var roleFilterPredicate = PredicateBuilder.True<Game>().And(g => g.GameAvailabilityStatus != AvailabilityStatus.NotForSale);

                // Equivalent to (conditionAbove && (searchPredicateConditions))
                searchPredicate = roleFilterPredicate.And(searchPredicate);
            }

            IQueryable<Game> gamesFiltered = db.Games.AsExpandable().Where(searchPredicate).OrderBy(g => g.Name);

            string platformName =
                await db.Platforms.
                    Where(p => p.PlatformCode == platform).
                    Select(p => p.PlatformName).
                    FirstOrDefaultAsync();

            string searchQuery = $"{title}, ";

            if (platformName != null)
            {
                searchQuery += $"{platformName}, ";
            }

            searchQuery += string.Join(", ", tags);
                
            ViewBag.SearchTerm = searchQuery.Trim(',', ' ');

            var gamesListViewModel = new GameListViewModel()
            {
                Games = await gamesFiltered.Skip((page - 1) * GAMES_PER_PAGE).Take(GAMES_PER_PAGE).ToListAsync(),
                CurrentPage = page,
                TotalPages = (int)Math.Ceiling(await gamesFiltered.CountAsync() / (float)GAMES_PER_PAGE)
            };

            return View("Index", gamesListViewModel);
        }

        /// <summary>
        ///     Displays the details for the specified game, including its SKUs and reviews
        /// </summary>
        /// <param name="id">
        ///     The id of the Game to show details for
        /// </param>
        /// <returns>
        ///     Details view if the Id is for a game
        ///     404 Not Found view if the Id couldn't be matched to a game
        ///     404 Not Found view if the Id is for a game marked as Not For Sale and the user isn't an employee or admin
        /// </returns>
        public async Task<ActionResult> Details(Guid? id)
        {
            if (id == null)
            {
                throw new HttpException((int)HttpStatusCode.NotFound, nameof(Game));
            }

            GameDetailsViewModel model = new GameDetailsViewModel();

            // TODO: When doing reviews, this will likely need to include all reviews too
            model.Game = await db.Games.Include(g => g.GameSKUs).FirstOrDefaultAsync(g => g.Id == id);

            if (model.Game == null)
            {
                throw new HttpException((int)HttpStatusCode.NotFound, nameof(Game));
            }

            if (User.IsEmployeeOrAdmin())
            {
                return View(model.Game);
            }

            // User is anonymous or member, don't show not for sale games
            if (model.Game.GameAvailabilityStatus == AvailabilityStatus.NotForSale)
            {
                throw new HttpException((int)HttpStatusCode.NotFound, nameof(Game));
            }

            // Remove formats that are not for sale unless the user is an employee
            model.Game.GameSKUs = model.Game.GameSKUs.
                Where(gp => gp.ProductAvailabilityStatus != AvailabilityStatus.NotForSale).
                ToList();

            Guid currentUserId = IIdentityExtensions.GetUserId(User.Identity);
            model.CurrentMember = await db.Members.FirstOrDefaultAsync(m => m.UserId == currentUserId);

            return View(model);
        }

        [ChildActionOnly]
        public ActionResult RenderPhysicalGameProduct(PhysicalGameProduct gameProduct, Member currentMember)
        {
            PhysicalGameProductViewModel model = new PhysicalGameProductViewModel
            {
                GameProduct = gameProduct
            };
            if (currentMember != null)
            {
                model.NewIsInCart = currentMember.Cart.Items.Any(i => i.ProductId == gameProduct.Id && i.IsNew);
                model.UsedIsInCart = currentMember.Cart.Items.Any(i => i.ProductId == gameProduct.Id && !i.IsNew);
                model.ProductIsOnWishlist = currentMember.Wishlist.Contains(gameProduct);
            }
            return PartialView("_PhysicalGameProductPartial", model);
        }

        /* TODO: Every action after this should be employee only */

        /// <summary>
        ///     Displays the Create Game view
        /// </summary>
        /// <returns>
        ///     The create game view
        /// </returns>
        public ActionResult Create()
        {
            ViewBag.ESRBRatingId = new SelectList(db.ESRBRatings, "RatingId", "Description");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create([Bind(Include = "Id,Name,GameAvailabilityStatus,ESRBRatingId,MinimumPlayerCount,MaximumPlayerCount,TrailerURL,ShortDescription,LongDescription,PrimaryImageURL")] Game game)
        {
            if (ModelState.IsValid)
            {
                db.Games.Add(game);
                await db.SaveChangesAsync();
                return RedirectToAction("Index");
            }

            ViewBag.ESRBRatingId = new SelectList(db.ESRBRatings, "RatingId", "Description", game.ESRBRatingId);
            return View(game);
        }

        // GET: Games/Edit/5
        public async Task<ActionResult> Edit(Guid? id)
        {
            if (id == null)
            {
                this.AddAlert(AlertType.Error, "Please select a game to edit.");
                return RedirectToAction("Index");
            }

            Game game = await db.Games.FindAsync(id);
            if (game == null)
            {
                this.AddAlert(AlertType.Error, "Please select a game to edit.");
                return RedirectToAction("Index");
            }

            ViewBag.ESRBRatingId = new SelectList(db.ESRBRatings, "RatingId", "Description", game.ESRBRatingId);

            return View(game);
        }

        // POST: Games/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit([Bind(Exclude = "Tags")] Game game, List<string> tags)
        {
            if (ModelState.IsValid)
            {
                //Can't do 'Tag Logic' here because game.Tags is null.
                //Can't set game.Tags = new List<Tag>() as that won't get saved to DB.

                //Save the game as binded (without changing tags)
                db.MarkAsModified(game);
                await db.SaveChangesAsync();

                //'Tag logic'
                //Get the game we just saved, including the tags this time
                game = await db.Games.Include(g => g.Tags).FirstAsync(g => g.Id == game.Id);

                //Remove all existing tags
                game.Tags.Clear();

                //Add tags
                foreach (var tag in tags)
                {
                    game.Tags.Add(db.Tags.First(t => t.Name == tag));
                }

                //Save the game again now with the tag info included
                await db.SaveChangesAsync();

                this.AddAlert(AlertType.Success, $"Your changes to '{game.Name}' were saved.");
                return RedirectToAction("Details", new {id = game.Id});
            }

            ViewBag.ESRBRatingId = new SelectList(db.ESRBRatings, "RatingId", "Description", game.ESRBRatingId);

            return View(game);
        }

        //For deleting Games
        // GET: Games/Delete/5
        public async Task<ActionResult> Delete(Guid? id)
        {
            if (id == null)
            {
                this.AddAlert(AlertType.Error, "Please select a game to delete.");
                return RedirectToAction("Index");
            }

            Game game = await db.Games.FindAsync(id);

            if (game == null)
            {
                return HttpNotFound();
            }

            return View(game);
        }

        // POST: Games/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteGameConfirmed(Guid id)
        {
            Game game = null;

            if (id != null)
            {
                game = await db.Games.FindAsync(id);
            }
            else
            {
                this.AddAlert(AlertType.Error, "No game selected");
            }

            if (game != null)
            {
                using (TransactionScope deleteScope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
                {
                    try
                    {
                        foreach (var item in game.GameSKUs)
                        {
                            db.GameProducts.Remove(item);
                        }
                    }
                    catch (Exception)
                    {
                        //display an error code about not being able to remove game products
                        //return back to delete confiramtion page
                        this.AddAlert(AlertType.Error, "Error");
                        return View();
                    }

                    try
        {
            db.Games.Remove(game);
            await db.SaveChangesAsync();
                        deleteScope.Complete();
                    }
                    catch (Exception)
                    {
                        //display an error about not being able to remove game
                        //return back to delete confiramtion page
                        this.AddAlert(AlertType.Error, "");
                        return View();
                    }
                }
            }
            else
            {
                //httpnotfound
            }

            return RedirectToAction("Index");
        }

        //for deleting game products
        // GET: Games/Delete/5
        public async Task<ActionResult> DeleteGameProduct(Guid? id)
        {
            if (id == null)
            {
                this.AddAlert(AlertType.Error, "Please select a game product to delete.");
            return RedirectToAction("Index");
        }

            GameProduct gameProduct = await db.GameProducts.FindAsync(id);
            if (gameProduct == null)
            {
                return HttpNotFound();
            }
            return View(gameProduct);
        }

        // POST: Games/Delete/5
        [HttpPost, ActionName("DeleteGameProduct")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteGameProductConfirmed(Guid id)
        {
            GameProduct gameProduct = null;

            if (id != null)
            {
                gameProduct = await db.GameProducts.FindAsync(id);
            }
            else
            {
                this.AddAlert(AlertType.Error, "No game selected");
            }

            if (gameProduct != null)
            {
                try
                {
                    db.GameProducts.Remove(gameProduct);
                    await db.SaveChangesAsync();
                }
                catch (Exception)
                {
                    //displays error message that product cant be deleted
                    //return back to delete confiramtion page
                    this.AddAlert(AlertType.Error, "Error happened");
                    return View();
                }
            }

            return RedirectToAction("Index");
        }

        #region GameProduct Actions
        public async Task<ActionResult> CreatePhysicalGameProduct(Guid? id)
        {
            if (id == null || !await db.Games.AnyAsync(g => g.Id == id))
            {
                this.AddAlert(AlertType.Error, "Please select a game to add a game product to.");
                return RedirectToAction("Index");
            }

            ViewBag.PlatformCode = new SelectList(db.Platforms, "PlatformCode", "PlatformName");
            ViewBag.DeveloperId = new SelectList(db.Companies, "Id", "Name");
            ViewBag.PublisherId = new SelectList(db.Companies, "Id", "Name");

            return View();
        }

        // POST: Games/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> CreatePhysicalGameProduct(Guid? id, [Bind] PhysicalGameProduct gameProduct)
        {
            if (id == null || !await db.Games.AnyAsync(g => g.Id == id))
            {
                this.AddAlert(AlertType.Error, "Please select a game to add a game product to.");
                return RedirectToAction("Index");
            }

            if (ModelState.IsValid)
            {
                var internalSku = db.GetNextPhysicalGameProductSku();

                gameProduct.InteralUsedSKU = $"0{internalSku}";
                gameProduct.InternalNewSKU = $"1{internalSku}";

                return await SaveGameProduct(id.Value, gameProduct);
            }

            ViewBag.PlatformCode = new SelectList(db.Platforms, "PlatformCode", "PlatformName");
            ViewBag.DeveloperId = new SelectList(db.Companies, "Id", "Name");
            ViewBag.PublisherId = new SelectList(db.Companies, "Id", "Name");

            return View(gameProduct);
        }

        public async Task<ActionResult> EditPhysicalGameProduct(Guid? id)
        {
            // TODO: Actually implement this

            return View(new PhysicalGameProduct());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> EditPhysicalGameProduct(Guid? id, PhysicalGameProduct gameProduct)
        {
            // TODO: Actually implement this

            return RedirectToAction("Index");
        }

        public async Task<ActionResult> CreateDownloadGameProduct(Guid? id)
        {
            if (id == null || !await db.Games.AnyAsync(g => g.Id == id))
            {
                this.AddAlert(AlertType.Error, "Please select a game to add a game product to.");
                return RedirectToAction("Index");
            }

            ViewBag.PlatformCode = new SelectList(db.Platforms, "PlatformCode", "PlatformName");
            ViewBag.DeveloperId = new SelectList(db.Companies, "Id", "Name");
            ViewBag.PublisherId = new SelectList(db.Companies, "Id", "Name");

            return View();
        }

        // POST: Games/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> CreateDownloadGameProduct(Guid? id, [Bind] DownloadGameProduct gameProduct)
        {
            if (id == null || !await db.Games.AnyAsync(g => g.Id == id))
            {
                this.AddAlert(AlertType.Error, "Please select a game to add a game product to.");
                return RedirectToAction("Index");
            }

            if (ModelState.IsValid)
            {
                return await SaveGameProduct(id.Value, gameProduct);
            }

            ViewBag.PlatformCode = new SelectList(db.Platforms, "PlatformCode", "PlatformName");
            ViewBag.DeveloperId = new SelectList(db.Companies, "Id", "Name");
            ViewBag.PublisherId = new SelectList(db.Companies, "Id", "Name");

            return View(gameProduct);
        }

        public async Task<ActionResult> EditDownloadGameProduct(Guid? id)
        {
            // TODO: Actually implement this

            return View(new DownloadGameProduct());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> EditDownloadGameProduct(Guid? id, DownloadGameProduct gameProduct)
        {
            // TODO: Actually implement this

            return RedirectToAction("Index");
        }

        private async Task<ActionResult> SaveGameProduct(Guid gameId, GameProduct gameProduct)
        {
            gameProduct.Id = Guid.NewGuid();
            gameProduct.Game = await db.Games.FindAsync(gameId);
            db.GameProducts.Add(gameProduct);

            await db.SaveChangesAsync();

            this.AddAlert(AlertType.Success, "Successfully added a new SKU.");

            return RedirectToAction("Details", "Games", new { id = gameId });
        }
        #endregion
    }
}
