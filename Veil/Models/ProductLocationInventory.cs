using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Veil.Models
{
    public class ProductLocationInventory
    {
        [Key]
        public string ProductId { get; set; }

        [Key]
        public string LocationId { get; set; }

        [ForeignKey(nameof(ProductId))]
        public virtual Product Product { get; set; }

        [ForeignKey(nameof(LocationId))]
        public virtual Location Location { get; set; }

        public int NewOnHand { get; set; }
        public int NewOnOrder { get; set; }

        public int UseOnHand { get; set; }
    }
}