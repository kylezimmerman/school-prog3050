/* TagViewModel.cs
 * Purpose: View model for listing and selecting Tags
 * 
 * Revision History:
 *      Kyle Zimmerman, 2015.11.03: Created
*/

using System.Collections.Generic;
using Veil.DataModels.Models;

namespace Veil.Models
{
    /// <summary>
    ///     View model for listing and selecting <see cref="Tag"/>s
    /// </summary>
    public class TagViewModel
    {
        /// <summary>
        ///     A list of all of the <see cref="Tag"/>s
        /// </summary>
        public IEnumerable<Tag> Selected { get; set; }

        /// <summary>
        ///     A list of the selected <see cref="Tag"/>s
        /// </summary>
        public IEnumerable<Tag> AllTags { get; set; }
    }
}