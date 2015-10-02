using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Veil.Models
{
    public enum ReviewStatus
    {
        Pending,
        Approved,
        Denied
    }

    public class Review
    {
        [Key]
        public Guid GameProductId { get; set; }

        [ForeignKey(nameof(GameProductId))]
        public GameProduct GameProduct { get; set; }

        [Key]
        public Guid MemberId { get; set; }

        [ForeignKey(nameof(MemberId))]
        public Member Member { get; set; }

        [Range(1, 5)]
        [Required]
        public int Rating { get; set; }

        public string ReviewText { get; set; }

        public ReviewStatus ReviewStatus { get; set; }
    }
}