using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Veil.Models
{
    public class Province
    {
        [Key]
        [Range(2, 2)]
        public string ProvinceCode { get; set; }

        [Key]
        [Range(2, 2)]
        public string CountryCode { get; set; }

        [Required]
        public string Name { get; set; }

        public decimal ProvincialTaxRate { get; set; }

        public string ProvincialTaxAcronym { get; set; }

        [ForeignKey(nameof(CountryCode))]
        public virtual Country Country { get; set; }
    }
}