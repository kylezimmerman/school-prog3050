using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Veil.Models
{
    public class Platform
    {
        [Key]
        public string PlatformCode { get; set; }

        [Required]
        public string PlatformName { get; set; }

        public virtual ICollection<Member> MembersFavoritePlatform { get; set; }
    }
}