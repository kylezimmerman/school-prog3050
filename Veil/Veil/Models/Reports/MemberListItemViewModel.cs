/* MemberListItemViewModel.cs
 * Purpose: View model for an item on the Member List report
 * 
 * Revision History:
 *      Drew Matheson, 2015.11.21: Created
 */ 

using System.ComponentModel.DataAnnotations;

namespace Veil.Models.Reports
{
    /// <summary>
    ///     View model for an item on the Member List report
    /// </summary>
    public class MemberListItemViewModel
    {
        /// <summary>
        ///     The username of the member for this row
        /// </summary>
        [Display(Name = "User")]
        public string UserName { get; set; }

        /// <summary>
        ///     The full name of the member for this row
        /// </summary>
        [Display(Name = "Name")]
        public string FullName { get; set; }

        /// <summary>
        ///     The number of orders from the member for this row
        /// </summary>
        [Display(Name = "Order Count")]
        public long OrderCount { get; set; }

        /// <summary>
        ///     The total amount the member for this row has spent on orders
        /// </summary>
        [Display(Name = "Total Sales")]
        public decimal TotalSpentOnOrders { get; set; }

        /// <summary>
        ///     The average amount spent on orders by the member this row is for
        /// </summary>
        [Display(Name = "Average Sale")]
        public decimal AverageOrderTotal { get; set; }
    }
}