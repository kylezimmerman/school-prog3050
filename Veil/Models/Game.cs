using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Veil.Models
{

    public class Game
    {
        public Guid Id { get; set; }

        [Required]
        public string Name { get; set; }

        [Required]
        public int GameStatusId { get; set; }

        [ForeignKey(nameof(GameStatusId))]
        public virtual GameStatus GameStatus { get; set; }

        [Required]
        public string ESRBRatingId { get; set; }

        [ForeignKey(nameof(ESRBRatingId))]
        public virtual ESRBRating Rating { get; set; }

        [Required]
        public int MinimumPlayerCount { get; set; }

        [Required]
        public int MaximumPlayerCount { get; set; }

        [Required]
        [DataType(DataType.Url)]
        public string TrailerURL { get; set; }

        [MaxLength(140)]
        [Required]
        public string ShortDescription { get; set; }

        public string LongDescription { get; set; }

        public virtual ICollection<ESRBContentDescriptor> ContentDescriptors { get; set; }

        public virtual ICollection<Tag> GameCategories { get; set; }

        public virtual ICollection<GameProduct> GameProducts { get; set; } // TODO: Consider name
    }
}