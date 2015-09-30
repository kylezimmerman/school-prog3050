using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Veil.Models
{
    public class WishList // TODO: Do we want this as a separate class or simply as a collection on Member?
    {
        [Key]
        public Guid MemberId { get; set; }

        public virtual ICollection<Product> Items { get; set; }
    }
}