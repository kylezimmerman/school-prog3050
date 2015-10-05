/* Review.cs
 * Purpose: An abstract base class for product reviews and an enumeration of the 
 *          moderation statuses for a review's text
 * 
 * Revision History:
 *      Drew Matheson, 2015.10.03: Created
 */ 

using System;
using System.ComponentModel.DataAnnotations;

namespace Veil.Models
{
    /// <summary>
    /// Enumeration of the moderation statuses for a review's text
    /// </summary>
    public enum ReviewStatus
    {
        Pending,
        Approved,
        Denied
    }

    // TODO: How do we handle editting reviews?
    /// <summary>
    /// Base class for a product review by a Member
    /// </summary>
    public abstract class Review
    {
        /// <summary>
        /// The Id of the Member who created this review
        /// </summary>
        [Key]
        public Guid MemberId { get; set; }

        /// <summary>
        /// Navigation property for the Member who created this review
        /// </summary>
        public Member Member { get; set; }

        /// <summary>
        /// A 1-5 rating
        /// </summary>
        [Range(1, 5)]
        [Required]
        public int Rating { get; set; }

        /// <summary>
        /// The review's text
        /// </summary>
        public string ReviewText { get; set; }

        /// <summary>
        /// The moderation status of the review
        /// </summary>
        public ReviewStatus ReviewStatus { get; set; }
    }
}