/* MemberCreditCard.cs
 * Purpose: A class for member credit card information
 * 
 * Revision History:
 *      Drew Matheson, 2015.10.15: Created
 */ 

using System;
using System.ComponentModel.DataAnnotations;
using Veil.DataModels.Validation;

namespace Veil.DataModels.Models
{
    /// <summary>
    ///     Member's Stored Credit Card Information
    /// </summary>
    public class MemberCreditCard
    {
        /// <summary>
        ///     The MemberCreditCard's Id
        /// </summary>
        [Key]
        public Guid Id { get; set; }

        /// <summary>
        ///     The Id of the Member this credit card belongs to
        /// </summary>
        public Guid MemberId { get; set; }

        /// <summary>
        ///     Navigation property for the Member this credit card belongs to
        /// </summary>
        public virtual Member Member { get; set; }

        /// <summary>
        ///     The Id for the Card as returned from Stripe
        /// </summary>
        [Required]
        [StringLength(maximumLength: 255, ErrorMessageResourceName = nameof(ErrorMessages.StringLength), ErrorMessageResourceType = typeof(ErrorMessages))]
        public string StripeCardId { get; set; }

        /// <summary>
        ///     The credit card's last 4 digits
        /// </summary>
        [Required]
        [StringLength(maximumLength: 4, MinimumLength = 4, ErrorMessageResourceName = nameof(ErrorMessages.StringLengthFixedLength), ErrorMessageResourceType = typeof(ErrorMessages))]
        public string Last4Digits { get; set; }

        /// <summary>
        ///     The month in which the credit card expires
        /// </summary>
        [Range(1, 12)]
        public int ExpiryMonth { get; set; }

        /// <summary>
        ///     The year in which the credit card expires
        /// </summary>
        public int ExpiryYear { get; set; }

        /// <summary>
        ///     The name of the cardholder as it appears on the credit card
        /// </summary>
        [Required]
        [StringLength(maximumLength: 255, ErrorMessageResourceName = nameof(ErrorMessages.StringLength), ErrorMessageResourceType = typeof(ErrorMessages))]
        public string CardholderName { get; set; }
    }
}