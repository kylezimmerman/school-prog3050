/* ReportsController.cs
 * Purpose: Controller for all the reports generated for the site
 * 
 * Revision History:
 *      Justin Coschi, 2015.10.20: Created
 */ 

using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Veil.DataAccess.Interfaces;
using Veil.DataModels;
using Veil.DataModels.Models;
using Veil.Models.Reports;

namespace Veil.Controllers
{
    [Authorize(Roles = VeilRoles.Authorize.Admin_Employee)]
    public class ReportsController : BaseController
    {
        private readonly IVeilDataAccess db;

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
            var gameList = await db.Games
                .Select(
                    g =>
                        new GameListViewModel
                        {
                            Game = g,
                            QuantitySold = db.WebOrders
                                .SelectMany(
                                    wo => wo.OrderItems
                                        .Where(oi => g.GameSKUs.Contains(oi.Product)))
                                .Select(oi => oi.Quantity)
                                .DefaultIfEmpty(0).Sum()
                        }
                ).OrderByDescending(g => g.QuantitySold).ToListAsync();

            return View(gameList);
        }

        [HttpPost]
        public async Task<ActionResult> GameList(DateTime start, DateTime? end)
        {
            end = end ?? DateTime.Now;

            var gameList = await db.Games
                .Select(
                    g =>
                        new GameListViewModel
                        {
                            Game = g,
                            QuantitySold = db.WebOrders
                                .Where(o => o.OrderDate >= start && o.OrderDate <= end)
                                .SelectMany(
                                    wo => wo.OrderItems
                                        .Where(oi => g.GameSKUs.Contains(oi.Product)))
                                .Select(oi => oi.Quantity)
                                .DefaultIfEmpty(0).Sum()
                        }
                ).OrderByDescending(g => g.QuantitySold).ToListAsync();

            return View(gameList);
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

        /// <summary>
        ///     Generates a report showing all <see cref="DataModels.Models.Member"/>s,
        ///     their names, total order count, total spent on orders, and average order
        /// </summary>
        /// <returns>
        ///     A view displaying the generated report
        /// </returns>
        [HttpGet]
        public async Task<ActionResult> MemberList()
        {
            List<MemberListItemViewModel> listItems = await db.Users.
                Where(u => u.Member != null).
                Select(u =>
                    new MemberListItemViewModel
                    {
                        UserName = u.UserName,
                        FullName = u.FirstName + " " + u.LastName,
                        OrderCount = db.WebOrders.Count(wo => wo.MemberId == u.Id),
                        TotalSpentOnOrders = db.WebOrders.
                            Where(wo => wo.MemberId == u.Id).
                            Select(wo => wo.OrderSubtotal + wo.ShippingCost + wo.TaxAmount).
                            DefaultIfEmpty(0).
                            Sum(),
                        AverageOrderTotal = db.WebOrders.
                            Where(wo => wo.MemberId == u.Id).
                            Select(wo => wo.OrderSubtotal + wo.ShippingCost + wo.TaxAmount).
                            DefaultIfEmpty(0).
                            Average()
                    }
                ).
                OrderByDescending(mli => mli.TotalSpentOnOrders).
                ThenByDescending(mli => mli.AverageOrderTotal).
                ToListAsync();

            return View(listItems);
        }

        /// <summary>
        ///     Generates a report showing all <see cref="DataModels.Models.Member"/>s,
        ///     their names, total order count, total spent on orders, and average order amount from 
        ///     <see cref="DataModels.Models.WebOrder"/>s between the <see cref="start"/> date and 
        ///     <see cref="end"/> date
        /// </summary>
        /// <param name="start">
        ///     The start date to filter the <see cref="DataModels.Models.WebOrder"/>s by
        /// </param>
        /// <param name="end">
        ///     Optional. The end date to filter the <see cref="DataModels.Models.WebOrder"/>s by. Defaults to Now.
        /// </param>
        /// <returns>
        ///     A view displaying the generated report
        /// </returns>
        [HttpPost]
        public async Task<ActionResult> MemberList(DateTime start, DateTime? end)
        {
            end = end ?? DateTime.Now;

            List<MemberListItemViewModel> listItems = await db.Users.
                Where(u => u.Member != null).
                Select(
                    u =>
                        new MemberListItemViewModel
                        {
                            UserName = u.UserName,
                            FullName = u.FirstName + " " + u.LastName,
                            OrderCount =
                                db.WebOrders.Count(
                                    wo =>
                                        wo.MemberId == u.Id && wo.OrderDate >= start && wo.OrderDate <= end),
                            TotalSpentOnOrders = db.WebOrders.
                        Where(wo => wo.MemberId == u.Id && wo.OrderDate >= start && wo.OrderDate <= end).
                        Select(wo => wo.OrderSubtotal + wo.ShippingCost + wo.TaxAmount).
                        DefaultIfEmpty(0).
                        Sum(),
                            AverageOrderTotal = db.WebOrders.
                        Where(wo => wo.MemberId == u.Id && wo.OrderDate >= start && wo.OrderDate <= end).
                        Select(wo => wo.OrderSubtotal + wo.ShippingCost + wo.TaxAmount).
                        DefaultIfEmpty(0).
                        Average()
                        }
                ).
                OrderByDescending(mli => mli.TotalSpentOnOrders).
                ThenByDescending(mli => mli.AverageOrderTotal).
                ThenByDescending(mli => mli.OrderCount).
                ToListAsync();

            return View(listItems);
        }

        //Member Detail report
        [HttpGet]
        public ActionResult MemberDetail(string userName)
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
        ///     A view presenting the wishlist counts for each game divided by platform
        /// </returns>
        [HttpGet]
        public async Task<ActionResult> Wishlist()
        {
            var model = new WishlistViewModel
            {
                Games = await db.Games.Select(g => new WishlistGameViewModel
                    {
                        Game = g,
                        Platforms = db.Platforms.Select(p => new WishlistGamePlatformViewModel
                            {
                                GamePlatform = p,
                                WishlistCount = g.GameSKUs
                                    .Where(gp => gp.Platform == p)
                                    .Select(gp => db.Members.Count(m => m.Wishlist.Contains(gp)))
                                    .DefaultIfEmpty(0).Sum()
                            }).OrderBy(m => m.GamePlatform.PlatformName),
                        WishlistCount = g.GameSKUs
                            .Select(gp => db.Members.Count(m => m.Wishlist.Contains(gp)))
                            .DefaultIfEmpty(0).Sum()
                    }).Where(m => m.WishlistCount > 0)
                    .OrderByDescending(m => m.WishlistCount).ToListAsync(),
                Platforms = await db.Platforms.Select(p => new WishlistPlatformViewModel
                    {
                        Platform = p,
                        WishlistCount = p.GameProducts
                            .Select(gp => db.Members.Count(m => m.Wishlist.Contains(gp)))
                            .DefaultIfEmpty(0).Sum()
                    }).OrderBy(p => p.Platform.PlatformName).ToListAsync()
            };

            return View(model);
        }

        /// <summary>
        ///     Displays more detailed information about the number of members who have a game's various formats wishlisted
        /// </summary>
        /// <param name="gameId">
        ///     The id of the Game to view the wishlist details of
        /// </param>
        /// <returns>
        ///     A view presenting the wishlist counts for each GameProduct under the specified Game
        /// </returns>
        [HttpGet]
        public async Task<ActionResult> WishlistDetail(Guid? gameId)
        {
            if (gameId == null)
            {
                throw new HttpException(NotFound, nameof(Game));
            }

            var model = await db.Games.Where(g => g.Id == gameId)
                .Select(g => new WishlistDetailGameViewModel
                {
                    Game = g,
                    GameProducts = g.GameSKUs.Select(gp => new WishlistDetailGameProductViewModel
                        {
                            GameProduct = gp,
                            WishlistCount = db.Members.Count(m => m.Wishlist.Contains(gp))
                        }).OrderByDescending(m => m.WishlistCount),
                    WishlistCount = g.GameSKUs
                        .Select(gp => db.Members.Count(m => m.Wishlist.Contains(gp)))
                        .DefaultIfEmpty(0).Sum()
                }).FirstOrDefaultAsync();

            if (model == null)
            {
                throw new HttpException(NotFound, nameof(Game));
            }

            return View(model);
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