using System;
using System.ComponentModel.DataAnnotations;

namespace Veil.DataModels.Models
{
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
        /// The credit card's number
        /// </summary>
        [Key]
        [MaxLength(19)]
        public string CardNumber { get; set; }

        /// <summary>
        /// The month in which the credit card expires
        /// </summary>
        public int ExpiryMonth { get; set; }

        /// <summary>
        /// The year in which the credit card expires
        /// </summary>
        public int ExpiryYear { get; set; }

        /// <summary>
        /// The security code found on the card
        /// </summary>
        [MaxLength(8)]
        public string CardSecurityCode { get; set; } 

        /// <summary>
        /// The name of the cardholder as it appears on the credit card
        /// </summary>
        [Required]
        [MaxLength(255)]
        public string CardholderName { get; set; }

        /// <summary>
        /// The billing information for this credit card
        /// </summary>
        public virtual CreditCardBillingInfo BillingInfo { get; set; }
    }
}