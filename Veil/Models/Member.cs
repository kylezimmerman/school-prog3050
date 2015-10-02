using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Veil.Models
{
    public class Member : Person
    {
        public virtual Cart Cart { get; set; }

        public virtual ICollection<Platform> FavoritePlatforms { get; set; }

        public virtual ICollection<Tag> FavoriteTags { get; set; }

        public virtual ICollection<Product> WishList { get; set; } 

        public virtual ICollection<Event> RegisteredEvents { get; set; } 
    }
}