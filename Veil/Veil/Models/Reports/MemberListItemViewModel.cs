/* MemberListItemViewModel.cs
 * Purpose: View model for an item on the Member List report
 * 
 * Revision History:
 *      Drew Matheson, 2015.11.21: Created
 */ 

using System.ComponentModel.DataAnnotations;

namespace Veil.Models.Reports
{
    public class MemberListItemViewModel
    {
        [Display(Name = "User")]
        public string UserName { get; set; }

        [Display(Name = "Name")]
        public string FullName { get; set; }

        [Display(Name = "Order Count")]
        public long OrderCount { get; set; }

        [Display(Name = "Total Sales")]
        public decimal TotalSpentOnOrders { get; set; }

        [Display(Name = "Average Sale")]
        public decimal AverageOrderTotal { get; set; }
    }
}