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
using Veil.Models;
using System.Transactions;

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

        [HttpGet]
        public async Task<ActionResult> Search()
        {
            SearchViewModel searchViewModel = new SearchViewModel();
            searchViewModel.Platforms = await db.Platforms.ToListAsync();
            searchViewModel.Tags = await db.Tags.ToListAsync();

            return View(searchViewModel);
        }

        // POST: Games/Search?{query-string}
        [HttpPost]
        public async Task<ActionResult> Search(List<string> tags, string keyword = "", string title = "", string platform = "")
        {
            //TODO: finish implementing Advanced Search
            //TODO: filter 'Not For Sale' depending on user status

            IQueryable<Game> gamesFiltered;

            keyword = keyword.Trim();
            title = title.Trim();
            platform = platform.Trim();
            tags = tags ?? new List<string>();
            tags.ForEach(t => t.Trim());

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
            GameDetailViewModels models = new GameDetailViewModels()
            {
                Game = await db.Games.FindAsync(id) ?? new Game(),
                // TODO: Make this not static
                EarliestRelease = new DateTime(2016, 12, 31)
            };

            // TODO: Check is game is "Not For Sale"

            if (models.Game == null)
            {
                return HttpNotFound();
            }

            return View(models);
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
                using (TransactionScope deleteScope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
                {
                    try
                    {
                        db.GameProducts.Remove(gameProduct);
                        await db.SaveChangesAsync();
                        deleteScope.Complete();

                    }
                    catch (Exception)
                    {
                        //displays error message that product cant be deleted
                        //return back to delete confiramtion page
                        this.AddAlert(AlertType.Error, "Error happened");
                        return View();
                    }
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
