/* CartViewModel.cs
 * Purpose: View model for the cart page
 * 
 * Revision History:
 *      Isaac West, 2015.11.17: Created
 */ 

using Veil.DataModels.Models;

namespace Veil.Models
{
    /// <summary>
    ///     View model for the cart page
    /// </summary>
    public class CartViewModel
    {
        /// <summary>
        ///     The cart
        /// </summary>
        public Cart Cart { get; set; }

        /// <summary>
        ///     The estimated cost to ship the items in the cart
        /// </summary>
        public decimal ShippingCost { get; set; }
    }
}