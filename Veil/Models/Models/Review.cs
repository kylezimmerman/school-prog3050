/* Review.cs
 * Purpose: An abstract base class for product reviews and an enumeration of the 
 *          moderation statuses for a review's text
 * 
 * Revision History:
 *      Drew Matheson, 2015.10.03: Created
 */

using System;
using System.ComponentModel.DataAnnotations;

namespace Veil.DataModels.Models
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

    /// <summary>
    /// Base class for a product review by a Member
    /// </summary>
    /// <typeparam name="TProduct">The type of the product being reviewed.</typeparam>
    /// <typeparam name="TKey">The type of the Key for the product being reviewed</typeparam>
    public abstract class Review<TProduct, TKey> where TProduct : Product
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
        [MaxLength(4000)] // Note: SQL Server maximum specifiable size. 
            // MAX (which is used above 4000) allows 2GB of text which I'd rather not allow the user to do
        public string ReviewText { get; set; }

        /// <summary>
        /// The moderation status of the review
        /// </summary>
        public ReviewStatus ReviewStatus { get; set; }

        /// <summary>
        /// The <see cref="TKey"/> Id for the <see cref="TProduct"/> this review is for
        /// </summary>
        [Key]
        public TKey ProductReviewedId { get; set; }

        /// <summary>
        /// Navigation property for the <see cref="TProduct"/> this review is for
        /// </summary>
        public virtual TProduct ProductReviewed { get; set; }
    }
}