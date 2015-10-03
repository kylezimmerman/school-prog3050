using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Veil.Models
{
    /// <summary>
    /// Product version of a game for a specific platform
    /// </summary>
    public abstract class GameProduct : Product
    {
        /// <summary>
        /// The Id of the publishing company for this version of the game
        /// </summary>
        [Required]
        public Guid PublisherId { get; set; }

        /// <summary>
        /// Navigation property for the publishing company for this version of the game
        /// </summary>
        public virtual Company Publisher { get; set; }

        /// <summary>
        /// The Id of the development company for this version of the game
        /// </summary>
        [Required]
        public Guid DeveloperId { get; set; }

        /// <summary>
        /// Navigation property for the development company for this version of the game
        /// </summary>
        public virtual Company Developer { get; set; }

        /// <summary>
        /// The platform code for the Platform this GameProduct is on
        /// </summary>
        [Required]
        public string PlatformCode { get; set; }

        /// <summary>
        /// Navigation property for the platform this GameProduct is on
        /// </summary>
        [ForeignKey(nameof(PlatformCode))]
        public virtual Platform Platform { get; set; }

        /// <summary>
        /// The Id of the Game this is a product version of
        /// </summary>
        [Required]
        public Guid GameId { get; set; }

        /// <summary>
        /// Navigation property for the Game this is a product version of
        /// </summary>
        [ForeignKey(nameof(GameId))]
        public virtual Game Game { get; set; }

        /// <summary>
        /// Gets the name for this GameProduct
        /// </summary>
        public override string Name => Game.Name;
    }
}