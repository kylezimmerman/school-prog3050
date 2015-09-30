using System.ComponentModel.DataAnnotations;

namespace Veil.Models
{
    public class Country
    {
        [Key]
        [Range(2, 2)]
        public string CountryCode { get; set; }

        [Required]
        public string CountryName { get; set; }

        public decimal FederalTaxRate { get; set; }

        public string FederalTaxAcronym { get; set; }
    }
}