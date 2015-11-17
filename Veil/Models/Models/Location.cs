// Location.cs
// Purpose: A class for business locations
// 
// Revision History:
//      Drew Matheson, 2015.10.03: Created
// 

using System;
using System.ComponentModel.DataAnnotations;
using Veil.DataModels.Validation;

namespace Veil.DataModels.Models
{
    /// <summary>
    ///     A business location
    /// </summary>
    public class Location
    {
        /// <summary>
        ///     The sitename for the online warehouse. As Id's change with Seeding, this is the most
        ///     unique way we can identify it.
        /// </summary>
        public const string ONLINE_WAREHOUSE_NAME = "Veil Online Warehouse";

        [Key]
        public virtual Guid Id { get; set; }

        /// <summary>
        ///     The Location's sequential location/store number
        /// </summary>
        public int LocationNumber { get; set; }

        /// <summary>
        ///     The location type name for this Location's type
        /// </summary>
        [Required]
        [MaxLength(64)]
        public string LocationTypeName { get; set; }

        /// <summary>
        ///     The Location's site name
        /// </summary>
        [Required]
        [MaxLength(128)]
        public string SiteName { get; set; }

        /// <summary>
        ///     The Location's phone number
        /// </summary>
        [Required]
        [DataType(DataType.PhoneNumber)]
        [RegularExpression(ValidationRegex.INPUT_PHONE)]
        [MaxLength(32)]
        public string PhoneNumber { get; set; }

        /// <summary>
        ///     The Location's fax number
        /// </summary>
        [DataType(DataType.PhoneNumber)]
        [RegularExpression(ValidationRegex.INPUT_PHONE)]
        [MaxLength(32)]
        public string FaxNumber { get; set; }

        /// <summary>
        ///     The Location's toll free number.
        ///     This will be null if the location doesn't have a toll free number
        /// </summary>
        [DataType(DataType.PhoneNumber)]
        [RegularExpression(ValidationRegex.INPUT_PHONE, ErrorMessage = "Must be in the format (800)555-0199 or 800-555-0199. Extensions must come after the phone number in the format \", ext. 5555\" with at least one digit.")]
        [MaxLength(32)]
        public string TollFreeNumber { get; set; }

        /// <summary>
        ///     The Address's street address, including apartment number
        /// </summary>
        [Required]
        [MaxLength(255)]
        public string StreetAddress { get; set; }

        /// <summary>
        ///     The Addresses optional post office box number
        /// </summary>
        [MaxLength(16)]
        public string POBoxNumber { get; set; }

        /// <summary>
        ///     The Address's city
        /// </summary>
        [Required]
        [MaxLength(255)]
        public string City { get; set; }

        /// <summary>
        ///     The Address's postal or zip code
        /// </summary>
        [Required]
        [DataType(DataType.PostalCode)]
        [MaxLength(16)]
        public string PostalCode { get; set; }

        /// <summary>
        ///     The province code for this Address's Province
        /// </summary>
        [StringLength(2, MinimumLength = 2)]
        [Required]
        public string ProvinceCode { get; set; }

        /// <summary>
        ///     Navigation property for this Address's Province
        /// </summary>
        public virtual Province Province { get; set; }

        /// <summary>
        ///     The country code for this Address's Country
        /// </summary>
        [StringLength(2, MinimumLength = 2)]
        [Required]
        public string CountryCode { get; set; }

        /// <summary>
        ///     Navigation property for this Address's Country
        /// </summary>
        public virtual Country Country { get; set; }
    }
}