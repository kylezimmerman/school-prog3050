/* LocationType.cs
 * Purpose: A class for specifying a location's type
 * 
 * Revision History:
 *      Drew Matheson, 2015.10.02: Created
 */

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Veil.DataModels.Validation;

namespace Veil.DataModels.Models
{
    /// <summary>
    ///     A location type for a retail location
    /// </summary>
    public class LocationType
    {
        /// <summary>
        ///     The location type's name
        /// </summary>
        /// <example>
        ///     <b>Examples:</b>
        ///     Head Office
        ///     Western Branch Office
        ///     Store
        /// </example>
        [Key]
        [Required]
        [StringLength(maximumLength: 64, ErrorMessageResourceName = nameof(ErrorMessages.StringLength), ErrorMessageResourceType = typeof(ErrorMessages))]
        public string LocationTypeName { get; set; }

        /// <summary>
        ///     Collection navigation property for all the locations with this type
        /// </summary>
        public ICollection<Location> Locations { get; set; }
    }
}