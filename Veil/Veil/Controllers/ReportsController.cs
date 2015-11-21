using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using LinqKit;
using Veil.DataAccess.Interfaces;
using Veil.DataModels.Models;
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
            var gameListItem = new GameListViewModel();
            var gameList = new List<GameListViewModel>();

            gameListItem = new GameListViewModel
            {
                Game = new Game(),
                QuantitySold = new int()
            };

            //var blerg = db.Games.Select(g => g.GameSKUs.SelectMany(gs => db.WebOrders.Select(wo => wo.OrderItems.Where(oi => oi.ProductId == gs.Id).Select(oi => oi.Quantity).DefaultIfEmpty(0).Sum()))).ToList();

            var gameSkus = db.Games.SelectMany(g => g.GameSKUs);
            var orderItems = db.WebOrders.SelectMany(wo => wo.OrderItems);
            var gamesSkusOrderItems = gameSkus.Join(orderItems, g => g.Id, o => o.ProductId,
                (product, item) => item);
            var quantities = gamesSkusOrderItems.GroupBy(o => o.ProductId).Select(a => new
            {
                Game = db.Games.FirstOrDefault(g => g.GameSKUs.Any(gp => gp.Id == a.Key)),
                Quantity = a.Sum(o => o.Quantity)
            });
            var quantitiesSum = quantities.GroupBy(q => q.Game.Id).Select(a => new
            {
                Game = a
            });

            return View();
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