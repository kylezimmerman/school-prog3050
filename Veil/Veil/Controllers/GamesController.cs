using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Net;
using System.Web;
using System.Web.Mvc;
using Veil.DataAccess;
using Veil.DataModels.Models;

namespace Veil.Controllers
{
    public class GamesController : Controller
    {
        private VeilDataContext db = new VeilDataContext();

        // GET: Games
        public async Task<ActionResult> Index()
        {
            var games = db.Games.Include(g => g.Rating);
            return View(await games.ToListAsync());
        }

        // GET: Games/Details/5
        public async Task<ActionResult> Details(Guid? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            //Game game = await db.Games.FindAsync(id);
            //if (game == null)
            //{
            //    return HttpNotFound();
            //}
            return View(new Game());
        }

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
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Game game = await db.Games.FindAsync(id);
            if (game == null)
            {
                return HttpNotFound();
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
                db.Entry(game).State = EntityState.Modified;
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
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
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

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
