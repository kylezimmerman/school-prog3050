/* Province.cs
 * Purpose: A class for province information
 * 
 * Revision History:
 *      Drew Matheson, 2015.10.02: Created
 */

using System.ComponentModel.DataAnnotations;
using Veil.DataModels.Validation;

namespace Veil.DataModels.Models
{
    /// <summary>
    ///     A province with its full name, tax information, and country information
    /// </summary>
    public class Province
    {
        /// <summary>
        ///     The provinces two letter code
        /// </summary>
        [Key]
        [StringLength(2, MinimumLength = 2, ErrorMessageResourceName = nameof(ErrorMessages.StringLengthFixedLength), ErrorMessageResourceType = typeof(ErrorMessages))]
        public string ProvinceCode { get; set; }

        /// <summary>
        ///     The country code for the province
        /// </summary>
        [Key]
        [StringLength(2, MinimumLength = 2, ErrorMessageResourceName = nameof(ErrorMessages.StringLengthFixedLength), ErrorMessageResourceType = typeof(ErrorMessages))]
        public string CountryCode { get; set; }

        /// <summary>
        ///     Navigation property for the province's country
        /// </summary>
        public virtual Country Country { get; set; }

        /// <summary>
        ///     The province's full name
        /// </summary>
        [Required]
        [StringLength(maximumLength: 255, ErrorMessageResourceName = nameof(ErrorMessages.StringLength), ErrorMessageResourceType = typeof(ErrorMessages))]
        public string Name { get; set; }

        /// <summary>
        ///     The provincial tax rate
        /// </summary>
        public decimal ProvincialTaxRate { get; set; }

        /// <summary>
        ///     The acronym for the province's tax
        /// </summary>
        [StringLength(maximumLength: 16, ErrorMessageResourceName = nameof(ErrorMessages.StringLength), ErrorMessageResourceType = typeof(ErrorMessages))]
        public string ProvincialTaxAcronym { get; set; }
    }
}