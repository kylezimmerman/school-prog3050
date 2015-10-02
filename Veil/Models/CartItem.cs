using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Veil.Models
{
    public class CartItem
    {
        // TODO: Setup EF to know this doesn't have a navigation property, and FK
        [Key]
        public Guid MemberId { get; set; }

        [Key, ForeignKey(nameof(Product))]
        public Guid ProductId { get; set; }

        public virtual Product Product { get; set; }

        /// <summary>
        /// Flag indicating if the product is a new or used product
        /// </summary>
        public bool IsNew { get; set; }

        public int Quantity { get; set; }
    }
}