/* Company.cs
 * Purpose: A class for game publisher and development companies
 * 
 * Revision History:
 *      Drew Matheson, 2015.10.02: Created
 */

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Veil.DataModels.Models
{
    /// <summary>
    /// Represents a game company and the GameProducts they have been involved with
    /// </summary>
    public class Company
    {
        /// <summary>
        /// The Id for the company
        /// </summary>
        [Key]
        public Guid Id { get; set; }

        /// <summary>
        /// The company's full name
        /// </summary>
        [Required]
        [StringLength(maximumLength:512, MinimumLength = 1)]
        public string Name { get; set; }

        /// <summary>
        /// Collection navigation property for the GameProducts the company has published
        /// </summary>
        public virtual ICollection<GameProduct> PublishedGameProducts { get; set; }

        /// <summary>
        /// Collection navigation property for the GameProducts the company has developed
        /// </summary>
        public virtual ICollection<GameProduct> DevelopedGameProducts { get; set; } 
    }
}