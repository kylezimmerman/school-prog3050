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
using System.Data.Entity.Infrastructure;
using System.Data.SqlClient;
using System.Transactions;
using System.Web;
using LinqKit;
using Veil.Extensions;
using Veil.DataModels;

namespace Veil.Controllers
{
    public class GamesController : BaseController
    {
        private const int GAMES_PER_PAGE = 10;

        private readonly IVeilDataAccess db;

        public GamesController(IVeilDataAccess veilDataAccess)
        {
            db = veilDataAccess;
        }

        public int GamesPerPage { get; set; } = GAMES_PER_PAGE;

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
            var viewModel = new GameListViewModel
            {
                CurrentPage = page
            };

            IQueryable<Game> games = db.Games;

            games = FilterOutInternalOnly(games).OrderBy(g => g.Name);

            viewModel.Games = await games.
                Skip((viewModel.CurrentPage - 1) * GamesPerPage).
                Take(GamesPerPage).
                ToListAsync();

            viewModel.TotalPages = 
                (int) Math.Ceiling(await games.CountAsync() / (float) GamesPerPage);

            return View(viewModel);
        }

        /// <summary>
        ///     Processes simple search game search. This is used by the nav-bar search
        /// </summary>
        /// <param name="keyword">
        ///     Fragment of a game title to filter by
        /// </param>
        /// <param name="page">
        ///     The page number being requested
        /// </param>
        /// <returns>
        ///     IQueryable of type 'Game' to Index view of Games controller.
        /// </returns>
        public async Task<ActionResult> Search(string keyword = "", int page = 1)
        {
            var viewModel = new GameListViewModel
            {
                CurrentPage = page
            };

            keyword = keyword.Trim();

            IQueryable<Game> gamesFiltered;

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                gamesFiltered = db.Games
                    .Where(g => g.Name.Contains(keyword));
            }
            else
            {
                gamesFiltered = db.Games;
            }

            gamesFiltered = FilterOutInternalOnly(gamesFiltered).OrderBy(g => g.Name);

            ViewBag.SearchTerm = keyword;

            viewModel.Games = await gamesFiltered.
                Skip((viewModel.CurrentPage - 1) * GamesPerPage).
                Take(GamesPerPage).
                ToListAsync();

            viewModel.TotalPages = 
                (int) Math.Ceiling(await gamesFiltered.CountAsync() / (float) GamesPerPage);

            return View("Index", viewModel);
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
        /// <param name="page">
        ///     The page number being requested
        /// </param>
        /// <returns>
        ///     Index view with the filtered results
        /// </returns>
        public async Task<ActionResult> AdvancedSearch(
            List<string> tags, string title = "", string platform = "", int page = 1)
        {
            page = page < 1 ? 1 : page;

            title = title.Trim();
            platform = platform.Trim();
            tags = tags ?? new List<string>();

            if (tags.Count == 0 && title == "" && platform == "")
            {
                AdvancedSearchViewModel advancedAdvancedSearchViewModel = new AdvancedSearchViewModel
                {
                    Platforms = await db.Platforms.ToListAsync()
                };

                return View(advancedAdvancedSearchViewModel);
            }

            for (int i = 0; i < tags.Count; i++)
            {
                string t = tags[i];
                t = t.Trim();
                tags[i] = t;
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
                searchPredicate = searchPredicate.Or(
                    g => g.GameSKUs.Any(gs => gs.PlatformCode == platform));
            }

            if (!User.IsEmployeeOrAdmin())
            {
                // Filter out any not for sale games
                // We are doing And, so we need the first to be true
                var roleFilterPredicate = PredicateBuilder.True<Game>().
                    And(g => g.GameAvailabilityStatus != AvailabilityStatus.NotForSale);

                // Equivalent to (conditionAbove && (searchPredicateConditions))
                searchPredicate = roleFilterPredicate.And(searchPredicate);
            }

            IQueryable<Game> gamesFiltered = db.Games.AsExpandable().
                Where(searchPredicate).
                OrderBy(g => g.Name);

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
                CurrentPage = page
            };

            gamesListViewModel.Games = await gamesFiltered.
                Skip((gamesListViewModel.CurrentPage - 1) * GamesPerPage).
                Take(GamesPerPage).
                ToListAsync();

            gamesListViewModel.TotalPages = 
                (int) Math.Ceiling(await gamesFiltered.CountAsync() / (float) GamesPerPage);

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
        ///     404 Not Found view if the Id is for a game marked as Not For Sale 
        ///         and the user isn't an employee or admin
        /// </returns>
        public async Task<ActionResult> Details(Guid? id)
        {
            if (id == null)
            {
                throw new HttpException(NotFound, nameof(Game));
            }

            Game game = await db.Games
                .Include(g => g.ContentDescriptors)
                .Include(g => g.GameSKUs)
                .Include(g => g.GameSKUs.Select(sku => sku.Reviews.Select(r => r.Member)))
                .FirstOrDefaultAsync(g => g.Id == id);

            if (game == null)
            {
                throw new HttpException(NotFound, nameof(Game));
            }

            if (User.IsEmployeeOrAdmin())
            {
                return View(game);
            }

            // User is anonymous or member, don't show not for sale games
            if (game.GameAvailabilityStatus == AvailabilityStatus.NotForSale)
            {
                throw new HttpException(NotFound, nameof(Game));
            }

            // Remove formats that are not for sale unless the user is an employee
            game.GameSKUs = game.GameSKUs.
                Where(gp => gp.ProductAvailabilityStatus != AvailabilityStatus.NotForSale).
                ToList();

            return View(game);
        }

        /// <summary>
        ///     Displays the Create Game view
        /// </summary>
        /// <returns>
        ///     The create game view
        /// </returns>
        [Authorize(Roles = VeilRoles.Authorize.Admin_Employee)]
        public ActionResult Create()
        {
            ViewBag.ESRBRatingId = new SelectList(db.ESRBRatings, "RatingId", "Description");
            return View();
        }

        [Authorize(Roles = VeilRoles.Authorize.Admin_Employee)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create([Bind(Exclude = nameof(Game.Tags) + "," + nameof(Game.ContentDescriptors))] Game game, List<string> tags, List<int> contentDescriptors)
        {
            if (ModelState.IsValid)
            {
                game.Tags = new List<Tag>();
                await SetTags(game, tags);

                game.ContentDescriptors = new List<ESRBContentDescriptor>();
                await SetESRBContentDescriptors(game, contentDescriptors);

                db.Games.Add(game);
                await db.SaveChangesAsync();

                return RedirectToAction("Details", new { id = game.Id });
            }

            ViewBag.ESRBRatingId = new SelectList(db.ESRBRatings, "RatingId", "Description", game.ESRBRatingId);
            return View(game);
        }

        [Authorize(Roles = VeilRoles.Authorize.Admin_Employee)]
        public async Task<ActionResult> Edit(Guid? id)
        {
            if (id == null)
            {
                throw new HttpException(NotFound, nameof(Game));
            }

            Game game = await db.Games.FindAsync(id);
            if (game == null)
            {
                throw new HttpException(NotFound, nameof(Game));
            }

            ViewBag.ESRBRatingId = new SelectList(db.ESRBRatings, "RatingId", "Description", game.ESRBRatingId);

            return View(game);
        }

        // POST: Games/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = VeilRoles.Authorize.Admin_Employee)]
        public async Task<ActionResult> Edit([Bind(Exclude = nameof(Game.Tags) + "," + nameof(Game.ContentDescriptors))] Game game, List<string> tags, List<int> contentDescriptors)
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
                // ReSharper disable once AccessToModifiedClosure
                game = await db.Games.Include(g => g.Tags).Include(g => g.ContentDescriptors).FirstAsync(g => g.Id == game.Id);

                await SetTags(game, tags);
                await SetESRBContentDescriptors(game, contentDescriptors);

                //Save the game again now with the tag info included
                await db.SaveChangesAsync();

                this.AddAlert(AlertType.Success, $"Your changes to '{game.Name}' were saved.");
                return RedirectToAction("Details", new {id = game.Id});
            }

            ViewBag.ESRBRatingId = new SelectList(db.ESRBRatings, "RatingId", "Description", game.ESRBRatingId);

            return View(game);
        }

        [Authorize(Roles = VeilRoles.Authorize.Admin_Employee)]
        public async Task<ActionResult> Delete(Guid? id)
        {
            if (id == null)
            {
                throw new HttpException(NotFound, nameof(Game));
            }

            Game game = await db.Games.FindAsync(id);

            if (game == null)
            {
                throw new HttpException(NotFound, nameof(Game));
            }

            return View(game);
        }

        [Authorize(Roles = VeilRoles.Authorize.Admin_Employee)]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteGameConfirmed(Guid id)
        {
            Game game = await db.Games.FindAsync(id);

            if (game == null)
            {
                throw new HttpException(NotFound, nameof(Game));
            }

            using (TransactionScope deleteScope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                try
                {
                    if (game.GameSKUs.Any())
                    {
                        foreach (var gameSKU in game.GameSKUs)
                        {
                            // TODO: This is new untested code.
                            db.ProductLocationInventories.RemoveRange(
                                await db.ProductLocationInventories.Where(
                                        pli =>
                                            pli.ProductId == gameSKU.Id &&
                                            pli.NewOnHand == 0 &&
                                            pli.UsedOnHand == 0 &&
                                            pli.NewOnOrder == 0).ToListAsync()
                            );
                            // TODO: End new untested code.
                        }

                        db.GameProducts.RemoveRange(game.GameSKUs);
                    }
                    
                    db.Games.Remove(game);
                    await db.SaveChangesAsync();
                    this.AddAlert(AlertType.Success, game.Name + " deleted succesfully");
                    deleteScope.Complete();
                }
                catch (DbUpdateException ex)
                {
                    // Get the exception which states if a foreign key constraint was violated
                    SqlException innermostException = ex.GetBaseException() as SqlException;

                    bool errorWasProvinceForeignKeyConstraint = false;

                    if (innermostException != null)
                    {
                        errorWasProvinceForeignKeyConstraint =
                            innermostException.Number == (int)SqlErrorNumbers.ConstraintViolation;
                    }

                    if (errorWasProvinceForeignKeyConstraint)
                    {
                        this.AddAlert(
                            AlertType.Error,
                            "Other portions of our system depend on this Game SKU's data." +
                                " Consider marking all of its SKUs as not for sale instead.");
                    }
                    else
                    {
                        this.AddAlert(AlertType.Error, "There was an error deleting " + game.Name + ".");
                    }

                    
                    return RedirectToAction("Delete", new { id = id });
                }
            }

                return RedirectToAction("Index");
            }

        /// <summary>
        /// Sets a Game's Tag to the provided list of tags by name. Note that this clears any existing tags.
        /// </summary>
        /// <param name="game">The game to set the tags on.</param>
        /// <param name="tagNames">A list of tag names to add to the game.</param>
        private async Task SetTags(Game game, List<string> tagNames)
        {
            //Clear any existing tags in the game
            game.Tags.Clear();

            if (tagNames == null)
            {
                return;
            }

            //Add all of the new tags by name
            foreach (var tagName in tagNames)
            {
                var tag = await db.Tags.FirstOrDefaultAsync(t => t.Name == tagName);
                if (tag != null)
                {
                    game.Tags.Add(tag);
                }
            }
        }

        /// <summary>
        /// Sets a Game's ESRB Content Descriptors to the provided list of content descriptors by Id. Note that this clears any existing descriptors.
        /// </summary>
        /// <param name="game">The game to set the tags on.</param>
        /// <param name="tagNames">A list of tag names to add to the game.</param>
        private async Task SetESRBContentDescriptors(Game game, List<int> contentDescriptors)
        {
            //Clear any existing tags in the game
            game.ContentDescriptors.Clear();

            if (contentDescriptors == null)
            {
                return;
            }

            var results = db.ESRBContentDescriptors.Where(e => contentDescriptors.Contains(e.Id));
            foreach (var esrbContentDescriptor in results)
            {
                game.ContentDescriptors.Add(esrbContentDescriptor);
            }
        }

        /// <summary>
        ///     Filters out not for sale games if the user isn't an employee or admin
        /// </summary>
        /// <param name="queryable">
        ///     The current Game IQueryable
        /// </param>
        /// <returns>
        ///     The filtered queryable
        /// </returns>
        private IQueryable<Game> FilterOutInternalOnly(IQueryable<Game> queryable)
        {
            if (!User.IsEmployeeOrAdmin())
            {
                return queryable.Where(g => g.GameAvailabilityStatus != AvailabilityStatus.NotForSale);
            }

            return queryable;
        }
    }
}
