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

namespace Veil.Controllers
{
    public class GamesController : Controller
    {
        protected readonly IVeilDataAccess db;

        public GamesController(IVeilDataAccess veilDataAccess)
        {
            db = veilDataAccess;
        }

        // GET: Games
        public async Task<ActionResult> Index()
        {
            var games = db.Games.Include(g => g.Rating);
            return View(await games.ToListAsync());
        }

        // GET: Games/Search?{query-string}
        public async Task<ActionResult> Search(string keyword = "", string title = "", string platform = "", string tags = "")
        {
            IQueryable<Game> gamesFiltered;

            keyword = keyword.Trim();
            title = title.Trim();
            platform = platform.Trim();
            tags = tags.Trim();

            if (keyword == "")
            {
                gamesFiltered = db.Games
                .Where(g => g.Name.Contains(title)
                    );

                ViewBag.SearchTerm = title;
            }
            else
            {
                gamesFiltered = db.Games
                .Where(g => g.Name.Contains(keyword));
                
                ViewBag.SearchTerm = keyword;
            }

            //TODO: finish implementing Advanced Search
            //TODO: filter 'Not For Sale' depending on user status

            return View("Index", await gamesFiltered.ToListAsync());
        }

        // GET: Games/Details/5
        public async Task<ActionResult> Details(Guid? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            // TODO: Remove the null coalesce and handle if id doesn't match. This supports both our test and real data.
            Game game = await db.Games.FindAsync(id) ?? new Game();

            if (game == null)
            {
                return HttpNotFound();
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
        public async Task<ActionResult> Edit([Bind(Include = "Id,Name,GameAvailabilityStatus,ESRBRatingId,MinimumPlayerCount,MaximumPlayerCount,TrailerURL,ShortDescription,LongDescription,PrimaryImageURL")] Game game)
        {
            if (ModelState.IsValid)
            {
                db.MarkAsModified(game);
                await db.SaveChangesAsync();
                return RedirectToAction("Index");
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

            return RedirectToAction("Details", "Games", new { id = gameId });
        }

        #endregion
    }
}
