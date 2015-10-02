using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Veil.Models
{
    public class Cart
    {
        [Key, ForeignKey(nameof(Member))]
        public Guid MemberId { get; set; }

        public virtual Member Member { get; set; }

        public virtual ICollection<CartItem> Items { get; set; }
    }
}