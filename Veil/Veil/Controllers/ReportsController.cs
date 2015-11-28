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
using Antlr.Runtime;
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

        /// <summary>
        /// Generates a report showing the number of sales of each Game object.
        /// </summary>
        /// <returns>A view displaying the generated report.</returns>
        [HttpGet]
        public async Task<ActionResult> GameList()
        {
            var viewModel = new DateFilteredListViewModel<GameListViewModel>
            {
                Items = await db.Games
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
                ).OrderByDescending(g => g.QuantitySold).ToListAsync()
            };

            return View(viewModel);
        }

        /// <summary>
        /// Generates a report showing the number of sales of each Game object, filtered by a date range.
        /// </summary>
        /// <param name="start">The date to start the filter range (inclusive).</param>
        /// <param name="end">The date to end the filter range (inclusive).</param>
        /// <returns>A view displaying the generated report.</returns>
        [HttpPost]
        public async Task<ActionResult> GameList(DateTime start, DateTime? end)
        {
            end = SetToEndOfDayIfInPast(end);

            var viewModel = new DateFilteredListViewModel<GameListViewModel>
            {
                StartDate = start,
                EndDate = end,
                Items = await db.Games
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
                    ).OrderByDescending(g => g.QuantitySold).ToListAsync()
            };

            return View(viewModel);
        }

        //Game Detail report
        [HttpGet]
        public async Task<ActionResult> GameDetail(Guid gameGuid)
        {
            var viewModel = new GameDetailViewModel
            {
                Game = await db.Games.FirstOrDefaultAsync(g => g.Id == gameGuid),
                Items = await db.GameProducts
                    .Where(gp => gp.GameId == gameGuid)
                    .Select(
                        gp =>
                            new GameDetailRowViewModel
                            {
                                GameProduct = gp,
                                NewQuantity = db.WebOrders
                                    .Where(
                                        wo =>
                                            wo.OrderStatus != OrderStatus.EmployeeCancelled ||
                                            wo.OrderStatus != OrderStatus.UserCancelled)
                                    .SelectMany(wo => wo.OrderItems
                                        .Where(oi => oi.ProductId == gp.Id))
                                    .Where(oi => oi.IsNew)
                                    .Select(oi => oi.Quantity)
                                    .DefaultIfEmpty(0).Sum(),
                                NewSales = db.WebOrders
                                    .Where(
                                        wo =>
                                            wo.OrderStatus != OrderStatus.EmployeeCancelled ||
                                            wo.OrderStatus != OrderStatus.UserCancelled)
                                    .SelectMany(wo => wo.OrderItems
                                        .Where(oi => oi.ProductId == gp.Id))
                                    .Where(oi => oi.IsNew)
                                    .Select(oi => oi.ListPrice * oi.Quantity)
                                    .DefaultIfEmpty(0).Sum(),
                                UsedQuantity = db.WebOrders
                                    .Where(
                                        wo =>
                                            wo.OrderStatus != OrderStatus.EmployeeCancelled ||
                                            wo.OrderStatus != OrderStatus.UserCancelled)
                                    .SelectMany(wo => wo.OrderItems
                                        .Where(oi => oi.ProductId == gp.Id))
                                    .Where(oi => !oi.IsNew)
                                    .Select(oi => oi.Quantity)
                                    .DefaultIfEmpty(0).Sum(),
                                UsedSales = db.WebOrders
                                    .Where(
                                        wo =>
                                            wo.OrderStatus != OrderStatus.EmployeeCancelled ||
                                            wo.OrderStatus != OrderStatus.UserCancelled)
                                    .SelectMany(wo => wo.OrderItems
                                        .Where(oi => oi.ProductId == gp.Id))
                                    .Where(oi => !oi.IsNew)
                                    .Select(oi => oi.ListPrice * oi.Quantity)
                                    .DefaultIfEmpty(0).Sum()
                            }
                    ).ToListAsync()
            };

            return View(viewModel);
        }

        [HttpPost]
        public async Task<ActionResult> GameDetail(Guid gameGuid, DateTime start, DateTime? end)
        {
            end = SetToEndOfDayIfInPast(end);

            var viewModel = new GameDetailViewModel
            {
                StartDate = start,
                EndDate = end,
                Game = await db.Games.FirstOrDefaultAsync(g => g.Id == gameGuid),
                Items = await db.GameProducts
                    .Where(gp => gp.GameId == gameGuid)
                    .Select(
                        gp =>
                            new GameDetailRowViewModel
                            {
                                GameProduct = gp,
                                NewQuantity = db.WebOrders
                                    .Where(wo => wo.OrderDate >= start && wo.OrderDate <= end)
                                    .Where(
                                        wo =>
                                            wo.OrderStatus != OrderStatus.EmployeeCancelled ||
                                            wo.OrderStatus != OrderStatus.UserCancelled)
                                    .SelectMany(wo => wo.OrderItems
                                        .Where(oi => oi.ProductId == gp.Id))
                                    .Where(oi => oi.IsNew)
                                    .Select(oi => oi.Quantity)
                                    .DefaultIfEmpty(0).Sum(),
                                NewSales = db.WebOrders
                                    .Where(wo => wo.OrderDate >= start && wo.OrderDate <= end)
                                    .Where(
                                        wo =>
                                            wo.OrderStatus != OrderStatus.EmployeeCancelled ||
                                            wo.OrderStatus != OrderStatus.UserCancelled)
                                    .SelectMany(wo => wo.OrderItems
                                        .Where(oi => oi.ProductId == gp.Id))
                                    .Where(oi => oi.IsNew)
                                    .Select(oi => oi.ListPrice * oi.Quantity)
                                    .DefaultIfEmpty(0).Sum(),
                                UsedQuantity = db.WebOrders
                                    .Where(wo => wo.OrderDate >= start && wo.OrderDate <= end)
                                    .Where(
                                        wo =>
                                            wo.OrderStatus != OrderStatus.EmployeeCancelled ||
                                            wo.OrderStatus != OrderStatus.UserCancelled)
                                    .SelectMany(wo => wo.OrderItems
                                        .Where(oi => oi.ProductId == gp.Id))
                                    .Where(oi => !oi.IsNew)
                                    .Select(oi => oi.Quantity)
                                    .DefaultIfEmpty(0).Sum(),
                                UsedSales = db.WebOrders
                                    .Where(wo => wo.OrderDate >= start && wo.OrderDate <= end)
                                    .Where(
                                        wo =>
                                            wo.OrderStatus != OrderStatus.EmployeeCancelled ||
                                            wo.OrderStatus != OrderStatus.UserCancelled)
                                    .SelectMany(wo => wo.OrderItems
                                        .Where(oi => oi.ProductId == gp.Id))
                                    .Where(oi => !oi.IsNew)
                                    .Select(oi => oi.ListPrice * oi.Quantity)
                                    .DefaultIfEmpty(0).Sum()
                            }
                    ).ToListAsync()
            };

            return View(viewModel);
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
            var viewModel = new DateFilteredListViewModel<MemberListItemViewModel>
            {
                Items = await db.Users.
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
                    ToListAsync()
            };

            return View(viewModel);
        }

        /// <summary>
        ///     Generates a report showing all <see cref="DataModels.Models.Member"/>s,
        ///     their names, total order count, total spent on orders, and average order amount from 
        ///     <see cref="DataModels.Models.WebOrder"/>s between the <see cref="start"/> date and 
        ///     <see cref="optionalEnd"/> date
        /// </summary>
        /// <param name="start">
        ///     The start date to filter the <see cref="DataModels.Models.WebOrder"/>s by
        /// </param>
        /// <param name="optionalEnd">
        ///     Optional. The end date to filter the <see cref="DataModels.Models.WebOrder"/>s by. Defaults to Now.
        /// </param>
        /// <returns>
        ///     A view displaying the generated report
        /// </returns>
        [HttpPost]
        public async Task<ActionResult> MemberList(DateTime start, DateTime? optionalEnd)
        {
            DateTime end = SetToEndOfDayIfInPast(optionalEnd);

            var viewModel = new DateFilteredListViewModel<MemberListItemViewModel>
            {
                StartDate = start,
                EndDate = end,
                Items = await db.Users.
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
                    ToListAsync()
            };

            return View(viewModel);
        }

        /// <summary>
        ///     Displays a report of all orders made by a member as well as member
        ///     information such as name, favorite tags, and favorite platforms
        /// </summary>
        /// <param name="username">
        ///     The username of the member to view information of
        /// </param>
        /// <returns>
        ///     A view presenting the member's information
        /// </returns>
        [HttpGet]
        public async Task<ActionResult> MemberDetail(string username)
        {
            if (username == null)
            {
                throw new HttpException(NotFound, nameof(Member));
            }

            var model = await db.Members.Select(m => new MemberDetailViewModel
            {
                UserName = m.UserAccount.UserName,
                FirstName = m.UserAccount.FirstName,
                LastName = m.UserAccount.LastName,
                WishlistCount = m.Wishlist.Count,
                FriendCount = m.ReceivedFriendships.Count(f => f.RequestStatus == FriendshipRequestStatus.Accepted) +
                    m.RequestedFriendships.Count(f => f.RequestStatus == FriendshipRequestStatus.Accepted),
                FavoritePlatforms = m.FavoritePlatforms,
                FavoriteTags = m.FavoriteTags,
                Items = m.WebOrders.Select(o => new MemberOrderViewModel
                {
                    OrderNumber = o.Id,
                    OrderDate = o.OrderDate,
                    OrderStatus = o.OrderStatus,
                    ProcessedDate = o.ProcessedDate,
                    Quantity = o.OrderItems.Sum(oi => oi.Quantity),
                    Subtotal = o.OrderSubtotal,
                    OrderTotal = o.OrderSubtotal + o.ShippingCost + o.TaxAmount
                }).OrderByDescending(o => o.OrderDate).ToList()
            }).FirstOrDefaultAsync(m => m.UserName == username);

            if (model == null)
            {
                throw new HttpException(NotFound, nameof(Member));
            }

            return View(model);
        }

        [HttpPost]
        public async Task<ActionResult> MemberDetail(string username, DateTime start, DateTime? end)
        {
            end = SetToEndOfDayIfInPast(end);

            if (username == null)
            {
                throw new HttpException(NotFound, nameof(Member));
            }

            var model = await db.Members.Select(m => new MemberDetailViewModel
            {
                StartDate = start,
                EndDate = end,
                UserName = m.UserAccount.UserName,
                FirstName = m.UserAccount.FirstName,
                LastName = m.UserAccount.LastName,
                WishlistCount = m.Wishlist.Count,
                FriendCount = m.ReceivedFriendships.Count(f => f.RequestStatus == FriendshipRequestStatus.Accepted) +
                    m.RequestedFriendships.Count(f => f.RequestStatus == FriendshipRequestStatus.Accepted),
                FavoritePlatforms = m.FavoritePlatforms,
                FavoriteTags = m.FavoriteTags,
                Items = m.WebOrders.Where(o => o.OrderDate >= start && o.OrderDate <= end)
                    .Select(o => new MemberOrderViewModel
                    {
                        OrderNumber = o.Id,
                        OrderDate = o.OrderDate,
                        OrderStatus = o.OrderStatus,
                        ProcessedDate = o.ProcessedDate,
                        Quantity = o.OrderItems.Sum(oi => oi.Quantity),
                        Subtotal = o.OrderSubtotal,
                        OrderTotal = o.OrderSubtotal + o.ShippingCost + o.TaxAmount
                    }).OrderByDescending(o => o.OrderDate).ToList()
            }).FirstOrDefaultAsync(m => m.UserName == username);

            return View(model);
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

        /// <summary>
        ///     Displays a report of all orders including order number, username,
        ///     number of items in the order, sums for item prices, shipping, tax,
        ///     and the order total
        /// </summary>
        /// <returns>
        ///     A view presenting order information
        /// </returns>
        [HttpGet]
        public async Task<ActionResult> Sales()
        {
            var model = new SalesViewModel
            {
                Items = await db.WebOrders.Select(o =>
                    new SalesOrderViewModel
                    {
                        OrderNumber = o.Id,
                        Username = o.Member.UserAccount.UserName,
                        Quantity = o.OrderItems.Sum(oi => oi.Quantity),
                        Subtotal = o.OrderSubtotal,
                        Shipping = o.ShippingCost,
                        Tax = o.TaxAmount,
                        OrderTotal = o.OrderSubtotal + o.ShippingCost + o.TaxAmount
                    }).OrderBy(o => o.OrderNumber)
                    .ToListAsync()
            };

            return View(model);
        }

        /// <summary>
        ///     Displays a report of all orders within a specified date range
        ///     including order number, username, number of items in the order,
        ///     sums for item prices, shipping, tax, and the order total
        /// </summary>
        /// <param name="start">
        ///     Orders before this date will not be shown
        /// </param>
        /// <param name="end">
        ///     Orders after this date will not be shown
        /// </param>
        /// <returns>
        ///     A view presenting order information filtered by date
        /// </returns>
        [HttpPost]
        public async Task<ActionResult> Sales(DateTime start, DateTime? end)
        {
            end = SetToEndOfDayIfInPast(end);

            var model = new SalesViewModel
            {
                StartDate = start,
                EndDate = end,
                Items = await db.WebOrders.Where(o => o.OrderDate >= start && o.OrderDate <= end)
                    .Select(o => new SalesOrderViewModel
                    {
                        OrderNumber = o.Id,
                        Username = o.Member.UserAccount.UserName,
                        Quantity = o.OrderItems.Sum(oi => oi.Quantity),
                        Subtotal = o.OrderSubtotal,
                        Shipping = o.ShippingCost,
                        Tax = o.TaxAmount,
                        OrderTotal = o.OrderSubtotal + o.ShippingCost + o.TaxAmount
                    }).OrderBy(o => o.OrderNumber)
                    .ToListAsync()
            };

            return View(model);
        }

        private DateTime SetToEndOfDayIfInPast(DateTime? value)
        {
            DateTime end = value?.Date ?? DateTime.Today;

            // Prevent under/overflowing MinValue and MaxValue
            return end == DateTime.MinValue.Date 
                ? end.AddDays(1).AddTicks(-1) 
                : end.AddTicks(-1).AddDays(1);
        }
    }

    
}