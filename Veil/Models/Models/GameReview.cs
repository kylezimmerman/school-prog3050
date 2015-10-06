/* GameReview.cs
 * Purpose: Class for a review of a specific game product
 * 
 * Revision History:
 *      Drew Matheson, 2015.10.03: Created
 */

using System;
using System.ComponentModel.DataAnnotations;

namespace Veil.DataModels.Models
{
    /// <summary>
    /// A review for a specific GameProduct by a Member
    /// </summary>
    public class GameReview : Review
    {
        /// <summary>
        /// The Id of the GameProduct the review is for
        /// </summary>
        [Key]
        public Guid GameProductId { get; set; }

        /// <summary>
        /// Navigation property for the GameProduct the review is for
        /// </summary>
        public virtual GameProduct GameProduct { get; set; }
    }
}