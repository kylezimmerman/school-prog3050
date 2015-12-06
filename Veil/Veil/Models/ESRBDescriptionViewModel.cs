/* ESRBDescriptionViewModel.cs
 * Purpose: View model for listing and selecting ESRB Content Descriptors
 * 
 * Revision History:
 *      Kyle Zimmerman, 2015.11.27: Created
 */ 

using System.Collections.Generic;
using Veil.DataModels.Models;

namespace Veil.Models
{
    /// <summary>
    ///     View model for listing and selecting ESRB Content Descriptors
    /// </summary>
    public class ESRBDescriptionViewModel
    {
        /// <summary>
        ///     An enumerable of all of the <see cref="ESRBContentDescriptor"/>s
        /// </summary>
        public IEnumerable<ESRBContentDescriptor> All { get; set; }

        /// <summary>
        ///     An enumerable of the selected <see cref="ESRBContentDescriptor"/>s
        /// </summary>
        public IEnumerable<ESRBContentDescriptor> Selected { get; set; }
    }
}