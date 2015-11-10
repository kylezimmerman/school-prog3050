using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;
using Veil.DataModels.Models;

namespace Veil.Models
{
    public class ManageAddressViewModel
    {
        /// <summary>
        /// The Address's street address, including apartment number
        /// </summary>
        [Required]
        [MaxLength(255)]
        [DisplayName("Street Address")]
        public string StreetAddress { get; set; }

        /// <summary>
        /// The Addresses optional post office box number
        /// </summary>
        [MaxLength(16)]
        [DisplayName("PO Box # (optional)")]
        public string POBoxNumber { get; set; }

        /// <summary>
        /// The Address's city
        /// </summary>
        [Required]
        [MaxLength(255)]
        public string City { get; set; }

        /// <summary>
        /// The Address's postal or zip code
        /// </summary>
        [Required]
        [DataType(DataType.PostalCode)]
        [MaxLength(16)]
        [DisplayName("Postal Code")]
        public string PostalCode { get; set; }

        /// <summary>
        /// The province code for this Address's Province
        /// </summary>
        [Required]
        [DisplayName("Province/State")]
        public string ProvinceCode { get; set; }

        /// <summary>
        /// The country code for this Address's Country
        /// </summary>

        [Required]
        [DisplayName("Country")]
        public string CountryCode { get; set; }

        /// <summary>
        ///     List of countries
        /// </summary>
        public IList<Country> Countries { get; set; }

        /// <summary>
        ///     Select list items of the Member's Addresses
        /// </summary>
        public IEnumerable<SelectListItem> Addresses { get; set; } 
    }
}