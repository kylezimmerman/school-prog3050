using System;
using System.Collections.Generic;
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
            var gameList = new List<GameListViewModel>();

            //var blerg = db.Games.Select(g => g.GameSKUs.SelectMany(gs => db.WebOrders.Select(wo => wo.OrderItems.Where(oi => oi.ProductId == gs.Id).Select(oi => oi.Quantity).DefaultIfEmpty(0).Sum()))).ToList();
            // Potential solution to the blerg above
            gameList = db.Games
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
                ).ToList();

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

        //Wishlist report
        [HttpGet]
        public ActionResult Wishlist()
        {
            return View();
        }

        //Sales report
        [HttpGet]
        public ActionResult Sales()
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