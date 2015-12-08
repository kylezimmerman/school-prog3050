/* Country.cs
 * Purpose: A class for country information
 * 
 * Revision History:
 *      Drew Matheson, 2015.10.02: Created
 */

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Veil.DataModels.Validation;

namespace Veil.DataModels.Models
{
    /// <summary>
    ///     A country with its full name and tax information
    /// </summary>
    public class Country
    {
        /// <summary>
        ///     The country's two letter code
        /// </summary>
        [Key]
        [StringLength(2, MinimumLength = 2, ErrorMessageResourceName = nameof(ErrorMessages.StringLengthFixedLength), ErrorMessageResourceType = typeof(ErrorMessages))]
        public string CountryCode { get; set; }

        /// <summary>
        ///     The country's full name
        /// </summary>
        [Required]
        [StringLength(255, ErrorMessageResourceName = nameof(ErrorMessages.StringLength), ErrorMessageResourceType = typeof(ErrorMessages))]
        public string CountryName { get; set; }

        /// <summary>
        ///     The country's federal tax rate
        /// </summary>
        public decimal FederalTaxRate { get; set; }

        /// <summary>
        ///     The acronym for the federal tax
        /// </summary>
        [StringLength(16, ErrorMessageResourceName = nameof(ErrorMessages.StringLength), ErrorMessageResourceType = typeof(ErrorMessages))]
        public string FederalTaxAcronym { get; set; }

        /// <summary>
        ///     Collection navigation property for this Country's provinces
        /// </summary>
        public ICollection<Province> Provinces { get; set; }
    }
}