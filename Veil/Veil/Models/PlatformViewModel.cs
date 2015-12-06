/* PlatformViewModel.cs
 * Purpose: View model for listing and selecting ESRB Content Descriptors
 * 
 * Revision History:
 *      Isaac West, 2015.11.27: Created
 */

using System.Collections.Generic;
using Veil.DataModels.Models;

namespace Veil.Models
{
    /// <summary>
    ///     View model for listing and selecting <see cref="Platform"/>s
    /// </summary>
    public class PlatformViewModel
    {
        /// <summary>
        ///     A list of all of the <see cref="Platform"/>s
        /// </summary>
        public IEnumerable<Platform> Selected { get; set; }

        /// <summary>
        ///     A list of the selected <see cref="Platform"/>s
        /// </summary>
        public IEnumerable<Platform> AllPlatforms { get; set; }
    }
}