using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Veil.Models
{
    public class Address
    {
        [Key]
        public string Id { get; set; } // GUID

        public string StreetAddress { get; set; }

        public string City { get; set; }

        public string PostalCode { get; set; }

        [Required]
        public string ProvinceCode { get; set; }

        [ForeignKey(nameof(ProvinceCode))]
        public virtual Province Province { get; set; }
    }
}