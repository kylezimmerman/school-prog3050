/* AdvancedSearchViewModel.cs
 * Purpose: View model for the advanced game search page
 * 
 * Revision History:
 *      Drew Matheson, 2015.11.08: Created
 */ 

using System.Collections.Generic;
using Veil.DataModels.Models;

namespace Veil.Models
{
    /// <summary>
    ///     View model for the advanced game search page
    /// </summary>
    public class AdvancedGameSearchViewModel
    {
        /// <summary>
        ///     Platforms used to create a select list which can be used for filtering
        /// </summary>
        public IEnumerable<Platform> Platforms { get; set; }
    }
}