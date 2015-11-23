using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using Veil.DataAccess.Interfaces;
using Veil.Models.Reports;

namespace Veil.Controllers
{
    public class ReportsController : BaseController
    {
        protected readonly IVeilDataAccess db;

        public ReportsController(IVeilDataAccess veilDataAccess)
        {
            db = veilDataAccess;
        }

        // GET: Reports
        public ActionResult Index()
        {
            return View();
        }

        //Game List report
        [HttpGet]
        public async Task<ActionResult> GameList()
        {
            // TODO: Remove these comments upon testing the query more
            //var blerg = db.Games.Select(g => g.GameSKUs.SelectMany(gs => db.WebOrders.Select(wo => wo.OrderItems.Where(oi => oi.ProductId == gs.Id).Select(oi => oi.Quantity).DefaultIfEmpty(0).Sum()))).ToList();
            // Potential solution to the blerg above
            var gameList = await db.Games
                .Select(g =>
                    new GameListViewModel
                    {
                        Game = g,
                        QuantitySold = db.WebOrders
                            .SelectMany(wo => wo.OrderItems
                                .Where(oi => g.GameSKUs.Contains(oi.Product)))
                            .Select(oi => oi.Quantity)
                            .DefaultIfEmpty(0).Sum()
                    }
                ).ToListAsync();

            return View(gameList);
        }

        [HttpPost]
        public ActionResult GameList(DateTime start, DateTime? end)
        {
            
            end = end ?? DateTime.Now;

            return View();
        }

        //Game Detail report
        [HttpGet]
        public ActionResult GameDetail(Guid gameGuid)
        {
            return View();
        }

        [HttpPost]
        public ActionResult GameDetail(DateTime start, DateTime? end)
        {
            end = end ?? DateTime.Now;

            return View();
        }

        //Member List report
        [HttpGet]
        public ActionResult MemberList()
        {
            return View();
        }

        [HttpPost]
        public ActionResult MemberList(DateTime start, DateTime? end)
        {
            end = end ?? DateTime.Now;

            return View();
        }

        //Member Detail report
        [HttpGet]
        public ActionResult MemberDetail()
        {
            return View();
        }

        [HttpPost]
        public ActionResult MemberDetail(DateTime start, DateTime? end)
        {
            end = end ?? DateTime.Now;

            return View();
        }

        /// <summary>
        ///     Displays a report of games and how many times they have been wishlisted for each platform
        /// </summary>
        /// <returns>
        ///     
        /// </returns>
        [HttpGet]
        public async Task<ActionResult> Wishlist()
        {
            // TODO: Can we do this all in one db call?
            var model = new WishlistViewModel
            {
                Games = await db.Games
                    .Select(g =>
                        new WishlistGameViewModel
                        {
                            Game = g,
                            Platforms = db.Platforms
                                .Select(p =>
                                    new WishlistGamePlatformViewModel
                                    {
                                        GamePlatform = p,
                                        WishlistCount = g.GameSKUs
                                            .Where(gp => gp.Platform == p)
                                            .Select(gp =>
                                                db.Members
                                                    .Count(m => m.Wishlist.Contains(gp))
                                            ).DefaultIfEmpty(0)
                                            .Sum()
                                    }
                                )
                        }
                    ).ToListAsync(),
                Platforms = await db.Platforms
                    .Select(p =>
                        new WishlistPlatformViewModel
                        {
                            Platform = p,
                            WishlistCount = p.GameProducts
                                .Select(gp =>
                                    db.Members
                                        .Count(m => m.Wishlist.Contains(gp))
                                ).DefaultIfEmpty(0)
                                .Sum()
                        }
                    ).OrderBy(p => p.Platform.PlatformName)
                    .ToListAsync()
            };

            return View(model);
        }

        [HttpGet]
        public async Task<ActionResult> WishlistDetail(Guid? gameId)
        {
            return View();
        }

        //Sales report
        [HttpGet]
        public async Task<ActionResult> Sales()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Sales(DateTime start, DateTime? end)
        {
            end = end ?? DateTime.Now;

            return View();
        }
    }
}