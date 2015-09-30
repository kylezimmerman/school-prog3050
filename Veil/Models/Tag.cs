using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Veil.Models
{
    public class Tag
    {
        [Key]
        public string Name { get; set; }

        public virtual ICollection<Member> MemberFavoriteCategory { get; set; }
    }
}