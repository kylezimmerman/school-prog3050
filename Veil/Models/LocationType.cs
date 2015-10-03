/* LocationType.cs
 * Purpose: A class for specifying a location's type
 * 
 * Revision History:
 *      Drew Matheson, 2015.10.02: Created
 */ 

using System.ComponentModel.DataAnnotations;

namespace Veil.Models
{
    /// <summary>
    /// A location type for a retail location
    /// </summary>
    public class LocationType
    {
        /// <summary>
        /// The location type's name
        /// <example>
        ///     Head Office
        ///     Western Branch Office
        ///     Store
        /// </example>
        /// </summary>
        [Key]
        [Required]
        public string LocationTypeName { get; set; }
    }
}