using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Veil.Models
{
    public class GameProduct : Product
    {
        [Required]
        public Guid PublisherId { get; set; }

        public virtual Company Publisher { get; set; }

        [Required]
        public Guid DeveloperId { get; set; }

        public virtual Company Developer { get; set; }

        [Required]
        public string PlatformCode { get; set; }

        [ForeignKey(nameof(PlatformCode))]
        public virtual Platform Platform { get; set; }

        public DateTime ReleaseDate { get; set; }

        [Required]
        public Guid GameId { get; set; }

        [ForeignKey(nameof(GameId))]
        public virtual Game Game { get; set; }
    }
}