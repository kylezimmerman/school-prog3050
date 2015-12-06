/* WishlistDetailViewModel.cs
 * Purpose: View models for the wishlist detail report
 * 
 * Revision History:
 *      Isaac West, 2015.11.23: Created
 */ 

using System.Collections.Generic;
using Veil.DataModels.Models;

namespace Veil.Models.Reports
{
    /// <summary>
    ///     View model for a game row in the Wishlist Detail report
    /// </summary>
    public class WishlistDetailGameViewModel
    {
        /// <summary>
        ///     The game this row is for
        /// </summary>
        public Game Game { get; set; }

        /// <summary>
        ///     Enumerable of WishListDetailGameProductViewModel containing each of the Games SKUs
        /// </summary>
        public IEnumerable<WishlistDetailGameProductViewModel> GameProducts { get; set; }

        /// <summary>
        ///     The number of times this game has been wishlisted
        /// </summary>
        public int? WishlistCount { get; set; }
    }

    /// <summary>
    ///     View model for each SKU of a game in the Wishlist report
    /// </summary>
    public class WishlistDetailGameProductViewModel
    {
        /// <summary>
        ///     The SKU itself
        /// </summary>
        public GameProduct GameProduct { get; set; }

        /// <summary>
        ///     The number of times this SKU has been wishlisted
        /// </summary>
        public int? WishlistCount { get; set; }
    }
}