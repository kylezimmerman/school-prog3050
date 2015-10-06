/* Tag.cs
 * Purpose: A class for tags used to categorize something
 * 
 * Revision History:
 *      Drew Matheson, 2015.10.03: Created
 */

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Veil.DataModels.Models
{
    /// <summary>
    /// A tag which can be used to categorize something
    /// </summary>
    public class Tag
    {
        /// <summary>
        /// The name for the tag. This also acts as the key
        /// </summary>
        [Key]
        public string Name { get; set; }

        /// <summary>
        /// Collection navigation property for the Member's who have this tag as one of their favorites
        /// </summary>
        public virtual ICollection<Member> MemberFavoriteCategory { get; set; }

        /// <summary>
        /// Collection navigation property for the Product's with this tag
        /// </summary>
        public virtual ICollection<Product> TaggedProducts { get; set; }
    }
}