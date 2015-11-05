using System;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Net;
using System.Web.Mvc;
using Veil.DataAccess.Interfaces;
using Veil.DataModels.Models;
using Veil.DataModels.Models.Identity;
using Veil.Helpers;
using Veil.Models;
using System.Collections.Generic;
using Microsoft.Practices.ObjectBuilder2;
using Veil.DataAccess;
using Veil.DataModels;
using Veil.DataModels;
using Veil.Models;

namespace Veil.Controllers
{
    public class GamesController : Controller
    {
        private const int GAMES_PER_PAGE = 50;

        protected readonly IVeilDataAccess db;

        public GamesController(IVeilDataAccess veilDataAccess)
        {
            db = veilDataAccess;
        }

        // GET: Games
        public async Task<ActionResult> Index()
        {
            var games = await db.Games.Include(g => g.Rating).ToListAsync();

            if (!User.IsInRole(VeilRoles.ADMIN_ROLE) && !User.IsInRole(VeilRoles.EMPLOYEE_ROLE))
            {
                games = games.Where(g => g.GameAvailabilityStatus != AvailabilityStatus.NotForSale).ToList();
            }

            int page;

            if (!int.TryParse(Request.QueryString?["page"], out page))
            {
                page = 1;
            }

            games = games.Skip((page - 1) * GAMES_PER_PAGE).Take(GAMES_PER_PAGE).ToList();

            return View(games);
        }

        // POST: Games/Search?{query-string}
        [HttpPost]
        public async Task<ActionResult> Search(string keyword = "")
        {
            keyword = keyword.Trim();

            IQueryable<Game>  gamesFiltered = db.Games
                .Where(g => g.Name.Contains(keyword));

            if (!User.IsInRole(VeilRoles.ADMIN_ROLE) && !User.IsInRole(VeilRoles.EMPLOYEE_ROLE))
            {
                gamesFiltered = gamesFiltered.Where(g => g.GameAvailabilityStatus != AvailabilityStatus.NotForSale);
            }

            ViewBag.SearchTerm = keyword;

            return View("Index", await gamesFiltered.ToListAsync());
        }

        // POST: Games/Search?{query-string}
        public async Task<ActionResult> AdvancedSearch(List<string> tags, string title = "", string platform = "")
        {
            title = title.Trim();
            platform = platform.Trim();
            tags = tags ?? new List<string>();
            tags.ForEach(t => t.Trim());

            if (tags.Count == 0 && title == "" && platform == "")
            {
                SearchViewModel searchViewModel = new SearchViewModel();
                searchViewModel.Platforms = await db.Platforms.ToListAsync();
                searchViewModel.Tags = await db.Tags.ToListAsync();

                return View(searchViewModel);
            }

            IQueryable<Game> gamesFiltered = db.Games
                .Where(g =>
                        (title != "" && g.Name.Contains(title)) ||
                        g.Tags.Any(t => tags.Contains(t.Name)) ||
                        g.GameSKUs.Any(gs => gs.Platform.PlatformCode == platform));

            if (!User.IsInRole(VeilRoles.ADMIN_ROLE) && !User.IsInRole(VeilRoles.EMPLOYEE_ROLE))
            {
                await gamesFiltered.LoadAsync();
                gamesFiltered = gamesFiltered.Where(g => g.GameAvailabilityStatus != AvailabilityStatus.NotForSale);
            }

            var searchQuery = ((title != "") ? title : "");

            searchQuery += ((platform != "") ? (", " + db.Platforms.First(p => p.PlatformCode == platform).PlatformName) : "");

            searchQuery = tags.Aggregate(searchQuery, (current, t) => current + (", " + t));

            searchQuery = searchQuery.Trim(',', ' ');
                
            ViewBag.SearchTerm = searchQuery;

            return View("Index", await gamesFiltered.ToListAsync());
        }

        // GET: Games/Details/5
        public async Task<ActionResult> Details(Guid? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            Game game = await db.Games.FindAsync(id);

            if (game == null)
            {
                this.AddAlert(AlertType.Error, "The selected game could not be displayed.");
                return RedirectToAction("Index");
            }
            else if (!User.IsInRole(VeilRoles.EMPLOYEE_ROLE) &&
                !User.IsInRole(VeilRoles.ADMIN_ROLE))
            {
                if (game.GameAvailabilityStatus == AvailabilityStatus.NotForSale)
                {
                    return View("Index");
                }
                // Remove formats that are not for sale unless the user is an employee
                if (game.GameSKUs.Any())
                {
                    game.GameSKUs = game.GameSKUs.Where(gp => gp.ProductAvailabilityStatus != AvailabilityStatus.NotForSale).ToList();
                }
            }

            return View(game);
        }

        // TODO: Every action after this should be employee only

        // GET: Games/Create
        public ActionResult Create()
        {
            ViewBag.ESRBRatingId = new SelectList(db.ESRBRatings, "RatingId", "Description");
            return View();
        }

        // POST: Games/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create([Bind(Include = "Id,Name,GameAvailabilityStatus,ESRBRatingId,MinimumPlayerCount,MaximumPlayerCount,TrailerURL,ShortDescription,LongDescription,PrimaryImageURL")] Game game)
        {
            if (ModelState.IsValid)
            {
                game.Id = Guid.NewGuid();
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
        public async Task<ActionResult> DeleteConfirmed(Guid id)
        {
            Game game = await db.Games.FindAsync(id);
            db.Games.Remove(game);
            await db.SaveChangesAsync();
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
