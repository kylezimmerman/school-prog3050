/* CreditCardBillingInfo.cs
 * Purpose: A class for credit card billing information
 * 
 * Revision History:
 *      Drew Matheson, 2015.10.06: Created
 */

using System;
using System.ComponentModel.DataAnnotations;

namespace Veil.DataModels.Models
{
    /// <summary>
    /// Credit Card Billing Information
    /// </summary>
    public class CreditCardBillingInfo
    {
        [Key]
        public Guid MemberId { get; set; }

        [Key]
        public string CardNumber { get; set; }

        /// <summary>
        /// The street address for the billing information
        /// </summary>
        public string StreetAddress { get; set; }

        /// <summary>
        /// The city for the billing information
        /// </summary>
        public string City { get; set; }

        /// <summary>
        /// The postal or zip code for the billing information
        /// </summary>
        public string PostalCode { get; set; }

        /// <summary>
        /// The province code for the billing information
        /// </summary>
        [StringLength(2, MinimumLength = 2)]
        [Required]
        public string ProvinceCode { get; set; }

        /// <summary>
        /// Navigation property for this CreditCardBillingInfo's Province
        /// </summary>
        public virtual Province Province { get; set; }

        /// <summary>
        /// The country code for the billing information
        /// </summary>
        [StringLength(2, MinimumLength = 2)]
        [Required]
        public string CountryCode { get; set; }

        /// <summary>
        /// Navigation property for this CreditCardBillingInfo's Country
        /// </summary>
        public virtual Country Country { get; set; }
    }
}