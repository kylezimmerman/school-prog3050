/* MemberCreditCard.cs
 * Purpose: A class for member credit card information
 * 
 * Revision History:
 *      Drew Matheson, 2015.10.15: Created
 */ 

using System;
using System.ComponentModel.DataAnnotations;

namespace Veil.DataModels.Models
{
    /// <summary>
    /// Member's Stored Credit Card Information
    /// </summary>
    public class MemberCreditCard
    {
        /// <summary>
        /// The Id of the Member this credit card belongs to
        /// </summary>
        [Key]
        public Guid MemberId { get; set; }

        /// <summary>
        /// Navigation property for the Member this credit card belongs to
        /// </summary>
        public virtual Member Member { get; set; }

        /// <summary>
        /// The Id for the Card as returned from Stripe
        /// </summary>
        [Key]
        [MaxLength(255)]
        public string StripeCardId { get; set; }

        /// <summary>
        /// The credit card's last 4 digits
        /// </summary>
        [Required]
        [StringLength(maximumLength: 4, MinimumLength = 4)]
        public string Last4Digits { get; set; }

        /// <summary>
        /// The month in which the credit card expires
        /// </summary>
        [Range(1, 12)]
        public int ExpiryMonth { get; set; }

        /// <summary>
        /// The year in which the credit card expires
        /// </summary>
        public int ExpiryYear { get; set; }

        /// <summary>
        /// The name of the cardholder as it appears on the credit card
        /// </summary>
        [Required]
        [MaxLength(255)]
        public string CardholderName { get; set; }
    }
}