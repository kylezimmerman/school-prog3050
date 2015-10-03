/* Platform.cs
 * Purpose: A class for game platforms
 * 
 * Revision History:
 *      Drew Matheson, 2015.10.03: Created
 */ 

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Veil.Models
{
    /// <summary>
    /// A gaming platform
    /// </summary>
    public class Platform
    {
        /// <summary>
        /// The code for the Platform
        /// </summary>
        [Key]
        [StringLength(5, MinimumLength = 1)]
        [Required]
        public string PlatformCode { get; set; }

        /// <summary>
        /// The Platform's full name
        /// </summary>
        [Required]
        public string PlatformName { get; set; }

        /// <summary>
        /// Collection navigation property for the Members who have this platform as one of their favorites
        /// </summary>
        public virtual ICollection<Member> MembersFavoritePlatform { get; set; }

        /// <summary>
        /// Collection navigation property for the GameProduct's on this platform
        /// </summary>
        public ICollection<GameProduct> GameProducts { get; set; }
    }
}