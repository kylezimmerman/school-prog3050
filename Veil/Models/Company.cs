using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Veil.Models
{
    public class Company
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public string Name { get; set; }

        public virtual ICollection<GameProduct> GameProducts { get; set; }
    }
}