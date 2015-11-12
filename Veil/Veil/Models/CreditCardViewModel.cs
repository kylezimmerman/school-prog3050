using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Veil.Models
{
    public abstract class CreditCardViewModel
    {
        /// <summary>
        /// The cardholder's full name
        /// </summary>
        [Required]
        [DisplayName("Cardholder's Full Name")]
        public string Name { get; set; }

        /// <summary>
        /// The credit card's number
        /// </summary>
        [MaxLength(19)]
        [DisplayName("Card Number")]
        [Required]
        public string Number { get; set; }

        /// <summary>
        /// The month in which the credit card expires
        /// </summary>
        [Range(1, 12)]
        [DisplayName("Expiry Month")]
        [Required]
        public int ExpirationMonth { get; set; }

        /// <summary>
        /// The year in which the credit card expires
        /// </summary>
        [DisplayName("Expiry Year")]
        [Required]
        public int ExpirationYear { get; set; }

        /// <summary>
        /// The security code found on the card
        /// </summary>
        [MaxLength(4)]
        [DisplayName("CVC Number")]
        public string Cvc { get; set; } 

        /// <summary>
        /// The street address for the billing information
        /// </summary>
        [Required]
        [MaxLength(255)]
        [DisplayName("Street Address")]
        public string AddressLine1 { get; set; }

        /// <summary>
        /// The city for the billing information
        /// </summary>
        [Required]
        [MaxLength(255)]
        [DisplayName("City")]
        public string AddressCity { get; set; }

        /// <summary>
        /// The postal or zip code for the billing information
        /// </summary>
        [Required]
        [DataType(DataType.PostalCode)]
        [MaxLength(10)]
        [DisplayName("Postal Code")]
        public string AddressZip { get; set; }

        /// <summary>
        /// The province code for the billing information
        /// </summary>
        [Required]
        [DisplayName("Province/State")]
        public string AddressState { get; set; }

        /// <summary>
        /// The country code for the billing information
        /// </summary>
        [Required]
        [DisplayName("Country")]
        public string AddressCountry { get; set; }
    }
}