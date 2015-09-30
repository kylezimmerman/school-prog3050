using System;
using System.ComponentModel.DataAnnotations;

namespace Veil.Models
{
    public abstract class Product
    {
        [Key]
        public Guid Id { get; set; }
    }
}