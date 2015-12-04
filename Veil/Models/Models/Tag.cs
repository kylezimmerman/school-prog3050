/* Tag.cs
 * Purpose: A class for tags used to categorize something
 * 
 * Revision History:
 *      Drew Matheson, 2015.10.03: Created
 */

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Veil.DataModels.Validation;

namespace Veil.DataModels.Models
{
    /// <summary>
    ///     A tag which can be used to categorize something
    /// </summary>
    public class Tag
    {
        /// <summary>
        ///     The name for the tag. This also acts as the key
        /// </summary>
        [Key]
        [StringLength(maximumLength: 64, ErrorMessageResourceName = nameof(ErrorMessages.StringLength), ErrorMessageResourceType = typeof(ErrorMessages))]
        public string Name { get; set; }

        /// <summary>
        ///     Collection navigation property for the Members who have this tag as one of their favorites
        /// </summary>
        public virtual ICollection<Member> MemberFavoriteCategory { get; set; }

        /// <summary>
        ///     Collection navigation property for the Games with this tag
        /// </summary>
        public virtual ICollection<Game> TaggedGames { get; set; }
    }
}