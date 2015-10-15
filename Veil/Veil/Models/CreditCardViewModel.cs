using System;
using System.ComponentModel.DataAnnotations;

namespace Veil.DataModels.Models
{
    public class CreditCardViewModel
    {
        /// <summary>
        /// The cardholder's full name
        /// </summary>
        [Required]
        public string Name { get; set; }

        /// <summary>
        /// The credit card's number
        /// </summary>
        [MaxLength(19)]
        public string Number { get; set; }

        /// <summary>
        /// The month in which the credit card expires
        /// </summary>
        [Range(1, 12)]
        public int ExpirationMonth { get; set; }

        /// <summary>
        /// The year in which the credit card expires
        /// </summary>
        public int ExpirationYear { get; set; }

        /// <summary>
        /// The security code found on the card
        /// </summary>
        [MaxLength(8)]
        public string Cvc { get; set; } 

        /// <summary>
        /// The Stripe Customer Id for the customer this credit card belongs to
        /// </summary>
        [Required]
        [MaxLength(255)]
        public string StripeCustomerId { get; set; }

        /// <summary>
        /// The street address for the billing information
        /// </summary>
        [Required]
        [MaxLength(255)]
        public string AddressLine1 { get; set; }

        /// <summary>
        /// The city for the billing information
        /// </summary>
        [Required]
        [MaxLength(255)]
        public string AddressCity { get; set; }

        /// <summary>
        /// The postal or zip code for the billing information
        /// </summary>
        [Required]
        [DataType(DataType.PostalCode)]
        [MaxLength(16)]
        public string AddressZip { get; set; }

        /// <summary>
        /// The province code for the billing information
        /// </summary>
        [StringLength(2, MinimumLength = 2)]
        [Required]
        public string AddressState { get; set; }

        /// <summary>
        /// The country code for the billing information
        /// </summary>
        [StringLength(2, MinimumLength = 2)]
        [Required]
        public string AddressCountry { get; set; }

        public string Object => "card";
    }
}