/* GameProduct.cs
 * Purpose: Abstract base class for a product version of a game on a specific platform
 * 
 * Revision History:
 *      Drew Matheson, 2015.10.03: Created
 */

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Veil.DataModels.Models
{
    /// <summary>
    /// Product SKU of a game for a specific platform
    /// </summary>
    public abstract class GameProduct : Product
    {
        /// <summary>
        /// The Id of the publishing company for this version of the game
        /// </summary>
        [Required]
        [DisplayName("Publisher")]
        public Guid PublisherId { get; set; }

        /// <summary>
        /// Navigation property for the publishing company for this version of the game
        /// </summary>
        [DisplayName("Publisher")]
        public virtual Company Publisher { get; set; }

        /// <summary>
        /// The Id of the development company for this version of the game
        /// </summary>
        [DisplayName("Developer")]
        [Required]
        public Guid DeveloperId { get; set; }

        /// <summary>
        /// Navigation property for the development company for this version of the game
        /// </summary>
        [DisplayName("Developer")]
        public virtual Company Developer { get; set; }

        /// <summary>
        /// The platform code for the Platform this GameProduct is on
        /// </summary>
        [Required]
        [DisplayName("Platform")]
        public string PlatformCode { get; set; }

        /// <summary>
        /// Navigation property for the platform this GameProduct is on
        /// </summary>
        [DisplayName("Platform")]
        public virtual Platform Platform { get; set; }

        /// <summary>
        /// The Id of the Game this is a product version of
        /// </summary>
        [Required]
        [DisplayName("Game")]
        public Guid GameId { get; set; }

        /// <summary>
        /// Navigation property for the Game this is a product version of
        /// </summary>
        [DisplayName("Game")]
        public virtual Game Game { get; set; }

        /// <summary>
        /// Gets the name for this GameProduct
        /// </summary>
        public override string Name => Game.Name;

        /// <summary>
        /// Gets the name for this GameProduct with its platform
        /// </summary>
        public virtual string NamePlatformDistinct => $"{Game.Name} ({Platform.PlatformName})";

        /// <summary>
        ///     The Game's SKU's average rating
        /// </summary>
        public double? AverageRating
        {
            get
            {
                return Reviews.Average(r => (double?) r.Rating);
            }
        }

        /// <summary>
        /// Collection navigation property for the reviews for this game product
        /// </summary>
        public virtual ICollection<GameReview> Reviews { get; set; }
    }
}