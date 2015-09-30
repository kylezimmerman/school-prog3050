using System.Collections.Generic;

namespace Veil.Models
{
    // TODO: Potentially in-memory only
    public class Cart
    {
        public string MemberId { get; set; }

        public ICollection<CartItem> Items { get; set; }
    }
}