/* WishlistViewModel.cs
 * Purpose: View models for the Wishlist report
 * 
 * Revision History:
 *      Isaac West, 2015.11.22: Created
 */ 

using System.Collections.Generic;
using System.Linq;
using Veil.DataModels.Models;

namespace Veil.Models.Reports
{
    /// <summary>
    ///     View model for the Wishlist report
    /// </summary>
    public class WishlistViewModel
    {
        /// <summary>
        ///     The <see cref="WishlistGameViewModel"/>s used to generate the rows of the report
        /// </summary>
        public IEnumerable<WishlistGameViewModel> Games { get; set; }

        /// <summary>
        ///     The platform summaries for the summary row
        /// </summary>
        public IEnumerable<WishlistPlatformViewModel> Platforms { get; set; }

        /// <summary>
        ///     The aggregate count for all games in the wishlist
        /// </summary>
        public int WishlistCount => Games.Sum(g => g.WishlistCount) ?? 0;
    }

    /// <summary>
    ///     View model for a game row in the wishlist report
    /// </summary>
    public class WishlistGameViewModel
    {
        /// <summary>
        ///     The game this row is for
        /// </summary>
        public Game Game { get; set; }

        /// <summary>
        ///     The wishlist counts for specific platforms
        /// </summary>
        public IEnumerable<WishlistGamePlatformViewModel> Platforms { get; set; }

        /// <summary>
        ///     The total wishlist count for the game
        /// </summary>
        public int? WishlistCount { get; set; }
    }

    /// <summary>
    ///     View model for the wishlist count of a Game on a specific platform
    /// </summary>
    public class WishlistGamePlatformViewModel
    {
        /// <summary>
        ///     The specific platform this game wishlist count is for
        /// </summary>
        public Platform GamePlatform { get; set; }

        /// <summary>
        ///     The wishlist count for the SKUs of the game for <see cref="GamePlatform"/>
        /// </summary>
        public int? WishlistCount { get; set; }
    }

    /// <summary>
    ///     View model for the platform summary row of the Wishlist report
    /// </summary>
    public class WishlistPlatformViewModel
    {
        /// <summary>
        ///     The platform the WishlistCount is for
        /// </summary>
        public Platform Platform { get; set; }

        /// <summary>
        ///     The count of how many times games on this platform have been wishlisted
        /// </summary>
        public int? WishlistCount { get; set; }
    }
}