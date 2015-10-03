using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Veil.Models
{
    public class Location : Address
    {
        public int LocationNumber { get; set; }

        [Required]
        public string LocationTypeName { get; set; }

        [ForeignKey(nameof(LocationTypeName))]
        public virtual LocationType LocationType { get; set; }

        public string SiteName { get; set; }

        public string PhoneNumber { get; set; }

        public string FaxNumber { get; set; }

        public string TollFreeNumber { get; set; }
    }
}