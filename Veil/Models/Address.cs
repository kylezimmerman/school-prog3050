/* Address.cs
 * Purpose: A abstract class which acts as a base for classes which store address information
 * 
 * Revision History:
 *      Drew Matheson, 2015.10.02: Created
 */ 

using System;
using System.ComponentModel.DataAnnotations;

namespace Veil.Models
{
    /// <summary>
    /// An address with street info, city, postal code, province, country, and optional PO box number
    /// </summary>
    public abstract class Address
    {
        [Key]
        public virtual Guid Id { get; set; }

        /// <summary>
        /// The Address's street address, including apartment number
        /// </summary>
        public string StreetAddress { get; set; }

        /// <summary>
        /// The Addresses optional post office box number
        /// </summary>
        public string POBoxNumber { get; set; }

        /// <summary>
        /// The Address's city
        /// </summary>
        public string City { get; set; }

        /// <summary>
        /// The Address's postal or zip code
        /// </summary>
        public string PostalCode { get; set; }

        /// <summary>
        /// The province code for this Address's Province
        /// </summary>
        [StringLength(2, MinimumLength = 2)]
        [Required]
        public string ProvinceCode { get; set; }

        /// <summary>
        /// Navigation property for this Address's Province
        /// </summary>
        public virtual Province Province { get; set; }

        /// <summary>
        /// The country code for this Address's Country
        /// </summary>
        [StringLength(2, MinimumLength = 2)]
        [Required]
        public string CountryCode { get; set; }

        /// <summary>
        /// Navigation property for this Address's Country
        /// </summary>
        public virtual Country Country { get; set; }
    }
}