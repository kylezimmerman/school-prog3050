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
using System.Web.Mvc;
using Veil.DataAccess.Interfaces;
using Veil.DataModels;
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
            // TODO: Remove these comments upon testing the query more
            //var blerg = db.Games.Select(g => g.GameSKUs.SelectMany(gs => db.WebOrders.Select(wo => wo.OrderItems.Where(oi => oi.ProductId == gs.Id).Select(oi => oi.Quantity).DefaultIfEmpty(0).Sum()))).ToList();
            // Potential solution to the blerg above
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
                Select(
                    u =>
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
                ThenByDescending(mli => mli.OrderCount).
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