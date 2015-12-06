/* MemberDetailViewModel.cs
 * Purpose: View models for the Member Detail report
 * 
 * Revision History:
 *      Isaac West, 2015.11.25: Created
 */ 

using System;
using System.Collections.Generic;
using System.Linq;
using Veil.DataModels.Models;

namespace Veil.Models.Reports
{
    /// <summary>
    ///    View model for the Member Detail report 
    /// </summary>
    public class MemberDetailViewModel : DateFilteredListViewModel<MemberOrderViewModel>
    {
        /// <summary>
        ///     The username of the member this report is for
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        ///     The first name of the member this report is for
        /// </summary>
        public string FirstName { get; set; }

        /// <summary>
        ///     The last name of the member this report is for
        /// </summary>
        public string LastName { get; set; }

        /// <summary>
        ///     The full name of the member this report is for
        /// </summary>
        public string FullName => $"{FirstName} {LastName}";

        /// <summary>
        ///     The number of items on the member's wishlist
        /// </summary>
        public int WishlistCount { get; set; }

        /// <summary>
        ///     The number of friends the member has
        /// </summary>
        public int FriendCount { get; set; }

        /// <summary>
        ///     The platforms the member has marked as favorites
        /// </summary>
        public ICollection<Platform> FavoritePlatforms { get; set; }

        /// <summary>
        ///     The tags the member has marked as favorites
        /// </summary>
        public ICollection<Tag> FavoriteTags { get; set; }

        /// <summary>
        ///     A comma separated list of the platform codes for the member's favorite platforms
        /// </summary>
        public string PlatformString => string.Join(", ", FavoritePlatforms.Select(p => p.PlatformCode));

        /// <summary>
        ///     A comma separated list of the tags for the member's favorite tags
        /// </summary>
        public string TagString => string.Join(", ", FavoriteTags.Select(t => t.Name));

        /// <summary>
        ///     The number of orders this member has between the filtered date range
        /// </summary>
        public int OrderCount => Items.Count(o => o.OrderStatus == OrderStatus.Processed);

        /// <summary>
        ///     The total number of items across all of the orders in the filtered date range
        /// </summary>
        public int TotalQuantity
            => Items.Where(o => o.OrderStatus == OrderStatus.Processed).Sum(o => o.Quantity);

        /// <summary>
        ///     The sum of the subtotals for the orders in the filtered date range
        /// </summary>
        public decimal ItemsSum
            => Items.Where(o => o.OrderStatus == OrderStatus.Processed).Sum(o => o.Subtotal);

        /// <summary>
        ///     The sum of the totals for the orders in the filtered date range
        /// </summary>
        public decimal Total
            => Items.Where(o => o.OrderStatus == OrderStatus.Processed).Sum(o => o.OrderTotal);
    }

    /// <summary>
    ///     View model for an order in the Member Detail report
    /// </summary>
    public class MemberOrderViewModel
    {
        /// <summary>
        ///     The order's number
        /// </summary>
        public long OrderNumber { get; set; }

        /// <summary>
        ///     The date for the order
        /// </summary>
        public DateTime OrderDate { get; set; }

        /// <summary>
        ///     The order's current status
        /// </summary>
        public OrderStatus OrderStatus { get; set; }

        /// <summary>
        ///     The date the order was processed, if it has been
        /// </summary>
        public DateTime? ProcessedDate { get; set; }

        /// <summary>
        ///     The number of items in the order
        /// </summary>
        public int Quantity { get; set; }

        /// <summary>
        ///     The subtotal for the order
        /// </summary>
        public decimal Subtotal { get; set; }

        /// <summary>
        ///     The order total
        /// </summary>
        public decimal OrderTotal { get; set; }
    }
}