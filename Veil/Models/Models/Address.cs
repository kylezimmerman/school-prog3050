/* Address.cs
 * Purpose: A model class representing an address. This is used by MemberAddress and WebOrder
 * 
 * Revision History:
 *      Drew Matheson, 2015.12.08: Created
 */ 

using System.ComponentModel.DataAnnotations;
using Veil.DataModels.Validation;

namespace Veil.DataModels.Models
{
    /// <summary>
    ///     An address, excluding Province/State and Country
    /// </summary>
    /// <remarks>
    ///     This is a complex type used as a property by <see cref="MemberAddress"/> and 
    ///     <see cref="WebOrder"/>
    /// </remarks>
    public class Address
    {
        /// <summary>
        ///     The Address's street address, including apartment number
        /// </summary>
        [Required]
        [StringLength(255, ErrorMessageResourceName = nameof(ErrorMessages.StringLength),
            ErrorMessageResourceType = typeof (ErrorMessages))]
        public string StreetAddress { get; set; }

        /// <summary>
        ///     The Addresses optional post office box number
        /// </summary>
        [StringLength(16, ErrorMessageResourceName = nameof(ErrorMessages.StringLength),
            ErrorMessageResourceType = typeof (ErrorMessages))]
        public string POBoxNumber { get; set; }

        /// <summary>
        ///     The Address's city
        /// </summary>
        [Required]
        [StringLength(255, ErrorMessageResourceName = nameof(ErrorMessages.StringLength),
            ErrorMessageResourceType = typeof (ErrorMessages))]
        public string City { get; set; }

        /// <summary>
        ///     The Address's postal or zip code
        /// </summary>
        [Required]
        [DataType(DataType.PostalCode)]
        [RegularExpression(ValidationRegex.STORED_POSTAL_CODE)]
        [StringLength(16, ErrorMessageResourceName = nameof(ErrorMessages.StringLength),
            ErrorMessageResourceType = typeof (ErrorMessages))]
        public string PostalCode { get; set; }
    }
}