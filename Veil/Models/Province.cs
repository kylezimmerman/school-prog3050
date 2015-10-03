/* Province.cs
 * Purpose: A class for province information
 * 
 * Revision History:
 *      Drew Matheson, 2015.10.02: Created
 */ 

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Veil.Models
{
    /// <summary>
    /// A province with its full name, tax information, and country information
    /// </summary>
    public class Province
    {
        /// <summary>
        /// The provinces two letter code
        /// </summary>
        [Key, Column(Order = 0)]
        [StringLength(2, MinimumLength = 2)]
        public string ProvinceCode { get; set; }

        /// <summary>
        /// The country code for the province
        /// </summary>
        [Key, Column(Order = 1)]
        [StringLength(2, MinimumLength = 2)]
        public string CountryCode { get; set; }

        /// <summary>
        /// Navigation property for the province's country
        /// </summary>
        [ForeignKey(nameof(CountryCode))]
        public virtual Country Country { get; set; }

        /// <summary>
        /// The province's full name
        /// </summary>
        [Required]
        public string Name { get; set; }

        /// <summary>
        /// The provincial tax rate
        /// </summary>
        public decimal ProvincialTaxRate { get; set; }

        /// <summary>
        /// The acronym for the province's tax
        /// </summary>
        public string ProvincialTaxAcronym { get; set; }
    }
}