/* ESRBRating.cs
 * Purpose: Class for ESRB ratings (e.g. E10+, T, M)
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
    ///     An ESRB rating for a game
    /// </summary>
    public class ESRBRating
    {
        /// <summary>
        ///     The Id for the rating
        /// </summary>
        /// <example>
        ///     <b>Examples:</b>
        ///     E10+, T, M
        /// </example>
        [Key]
        [StringLength(maximumLength: 8, ErrorMessageResourceName = nameof(ErrorMessages.StringLength), ErrorMessageResourceType = typeof(ErrorMessages))]
        public string RatingId { get; set;
        }

        /// <summary>
        ///     The longer description for the rating
        /// </summary>
        /// <example>
        ///     <b>Examples:</b>
        ///     Everyone 10+, Teen, Mature
        /// </example>
        [Required]
        [StringLength(maximumLength: 64, ErrorMessageResourceName = nameof(ErrorMessages.StringLength), ErrorMessageResourceType = typeof(ErrorMessages))]
        public string Description { get; set; }

        /// <summary>
        ///     The URL for the image representation of this rating
        /// </summary>
        [DataType(DataType.Url)]
        [Url]
        [StringLength(maximumLength: 2048, ErrorMessageResourceName = nameof(ErrorMessages.StringLength), ErrorMessageResourceType = typeof(ErrorMessages))]
        public string ImageURL { get; set; }

        /// <summary>
        ///     The minimum suitable age for the game
        /// </summary>
        [Required]
        public byte MinimumAge { get; set; }

        /// <summary>
        ///     Collection navigation property for games with this rating
        /// </summary>
        public virtual ICollection<Game> Games { get; set; }
    }
}