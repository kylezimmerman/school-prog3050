/* SalesViewModel.cs
 * Purpose: View models for the Sales report
 * 
 * Revision History:
 *      Isaac West, 2015.11.25: Created
 */ 

using System.Linq;

namespace Veil.Models.Reports
{
    /// <summary>
    ///     View model for the Sales report
    /// </summary>
    public class SalesViewModel : DateFilteredListViewModel<SalesOrderViewModel>
    {
        /// <summary>
        ///     The number of orders in the report
        /// </summary>
        public int OrderCount => Items.Count();

        /// <summary>
        ///     The total number of items across all of the orders in the report
        /// </summary>
        public int TotalQuantity => Items.Sum(o => o.Quantity);

        /// <summary>
        ///     The sum of the subtotals for all the orders in the report
        /// </summary>
        public decimal ItemsSum => Items.Sum(o => o.Subtotal);

        /// <summary>
        ///     The total shipping cost for all the orders in the report
        /// </summary>
        public decimal ShippingSum => Items.Sum(o => o.Shipping);

        /// <summary>
        ///     The total tax amount for all the orders in the report
        /// </summary>
        public decimal TaxSum => Items.Sum(o => o.Tax);

        /// <summary>
        ///     The sum of the totals for the orders in the report
        /// </summary>
        public decimal Total => Items.Sum(o => o.OrderTotal);
    }

    /// <summary>
    ///     View model for a row of the Sales report
    /// </summary>
    public class SalesOrderViewModel
    {
        /// <summary>
        ///     The order's number
        /// </summary>
        public long OrderNumber { get; set; }

        /// <summary>
        ///     The username of the member this report item is for
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        ///     The number of items in the order
        /// </summary>
        public int Quantity { get; set; }

        /// <summary>
        ///     The subtotal for the order
        /// </summary>
        public decimal Subtotal { get; set; }

        /// <summary>
        ///     The shipping cost for the order
        /// </summary>
        public decimal Shipping { get; set; }

        /// <summary>
        ///     The tax total for the order
        /// </summary>
        public decimal Tax { get; set; }

        /// <summary>
        ///     The order total
        /// </summary>
        public decimal OrderTotal { get; set; }
    }
}