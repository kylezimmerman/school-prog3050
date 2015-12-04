using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Veil.DataModels.Validation;

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
        [StringLength(maximumLength: 19, ErrorMessageResourceName = nameof(ErrorMessages.StringLength), ErrorMessageResourceType = typeof(ErrorMessages))]
        [DisplayName("Card Number")]
        [Required]
        public string Number { get; set; }

        /// <summary>
        /// The month in which the credit card expires
        /// </summary>
        [Range(1, 12, ErrorMessageResourceName = nameof(ErrorMessages.Range), ErrorMessageResourceType = typeof(ErrorMessages))]
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
        [StringLength(maximumLength: 4, ErrorMessageResourceName = nameof(ErrorMessages.StringLength), ErrorMessageResourceType = typeof(ErrorMessages))]
        [DisplayName("CVC Number")]
        public string Cvc { get; set; } 

        /// <summary>
        /// The street address for the billing information
        /// </summary>
        [Required]
        [StringLength(maximumLength: 255, ErrorMessageResourceName = nameof(ErrorMessages.StringLength), ErrorMessageResourceType = typeof(ErrorMessages))]
        [DisplayName("Street Address")]
        public string AddressLine1 { get; set; }

        /// <summary>
        /// The city for the billing information
        /// </summary>
        [Required]
        [StringLength(maximumLength: 255, ErrorMessageResourceName = nameof(ErrorMessages.StringLength), ErrorMessageResourceType = typeof(ErrorMessages))]
        [DisplayName("City")]
        public string AddressCity { get; set; }

        /// <summary>
        /// The postal or zip code for the billing information
        /// </summary>
        [Required]
        [DataType(DataType.PostalCode)]
        [StringLength(maximumLength: 10, ErrorMessageResourceName = nameof(ErrorMessages.StringLength), ErrorMessageResourceType = typeof(ErrorMessages))]
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