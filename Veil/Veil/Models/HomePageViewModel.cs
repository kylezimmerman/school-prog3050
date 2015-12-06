/* HomePageViewModel.cs
 * Purpose: View model for the home page
 * 
 * Revision History:
 *      Isaac West, 2015.11.03: Created
 */ 

using System.Collections.Generic;
using Veil.DataModels.Models;

namespace Veil.Models
{
    /// <summary>
    ///     View model for HomeController's index page
    /// </summary>
    public class HomePageViewModel
    {
        /// <summary>
        ///     The games to display in the coming soon section
        /// </summary>
        public IEnumerable<Game> ComingSoon { get; set; }

        /// <summary>
        ///     The games to display in the new releases section
        /// </summary>
        public IEnumerable<Game> NewReleases { get; set; }
    }
}