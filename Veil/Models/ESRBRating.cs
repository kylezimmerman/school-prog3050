using System.ComponentModel.DataAnnotations;

namespace Veil.Models
{
    public class ESRBRating
    {
        [Key]
        public string RatingId { get; set; }

        [Required]
        public string Description { get; set; }
    }
}