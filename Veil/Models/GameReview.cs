using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Veil.Models
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
        [ForeignKey(nameof(GameProductId))]
        public virtual GameProduct GameProduct { get; set; }
    }
}