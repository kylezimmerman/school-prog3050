using System.ComponentModel.DataAnnotations;
using Veil.DataModels.Validation;

namespace Veil.DataModels.Models
{
    public class Address
    {
        /// <summary>
        /// The Address's street address, including apartment number
        /// </summary>
        [Required]
        [MaxLength(255)]
        public string StreetAddress { get; set; }

        /// <summary>
        /// The Addresses optional post office box number
        /// </summary>
        [MaxLength(16)]
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
        [RegularExpression(ValidationRegex.STORED_POSTAL_CODE)]
        [MaxLength(16)]
        public string PostalCode { get; set; }
    }
}
