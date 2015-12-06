/* PhysicalGameProductViewModel.cs
 * Purpose: View model for PhysicalGameProduct
 * 
 * Revision History:
 *      Isaac West, 2015.11.07: Created
 */ 

using Veil.DataModels.Models;

namespace Veil.Models
{
    /// <summary>
    ///     View model for <see cref="PhysicalGameProduct"/>
    /// </summary>
    public class PhysicalGameProductViewModel
    {
        /// <summary>
        ///     The game product
        /// </summary>
        public PhysicalGameProduct GameProduct { get; set; }

        /// <summary>
        ///     Flag indicating if a new version of the <see cref="PhysicalGameProduct"/> is 
        ///     in the user's cart
        /// </summary>
        public bool NewIsInCart { get; set; }

        /// <summary>
        ///     Flag indicating if a used version of the <see cref="PhysicalGameProduct"/> is 
        ///     in the user's cart
        /// </summary>
        public bool UsedIsInCart { get; set; }

        /// <summary>
        ///     Flag indicating if the product is on the user's wishlist
        /// </summary>
        public bool ProductIsOnWishlist { get; set; }
    }
}