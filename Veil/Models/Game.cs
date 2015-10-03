/* Game.cs
 * Purpose: A class for information about a game that isn't specific to a product version of it
 * 
 * Revision History:
 *      Drew Matheson, 2015.10.02: Created
 */ 

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Veil.Models.Validation;

namespace Veil.Models
{
    /// <summary>
    /// Enumeration of the availability status for games
    /// </summary>
    public enum GameAvailabilityStatus
    {
        /// <summary>
        /// The game is available for pre-order
        /// </summary>
        [Description("Pre-order")]
        PreOrder,

        /// <summary>
        /// The game is available for purchase
        /// </summary>
        Available,

        /// <summary>
        /// The game has been discontinued
        /// </summary>
        Discontinued
    }

    /// <summary>
    /// Non product-specific information about a game
    /// </summary>
    public class Game
    {
        /// <summary>
        /// The Game's Id
        /// </summary>
        [Key]
        public Guid Id { get; set; }

        /// <summary>
        /// The Game's name
        /// </summary>
        [Required]
        public string Name { get; set; }

        /// <summary>
        /// The Game's availability status
        /// </summary>
        public virtual GameAvailabilityStatus GameAvailabilityStatus { get; set; }

        /// <summary>
        /// The Id for the Game's ESRB rating
        /// </summary>
        [Required]
        public string ESRBRatingId { get; set; }

        /// <summary>
        /// Navigation property for the Game's ESRB rating
        /// </summary>
        [ForeignKey(nameof(ESRBRatingId))]
        public virtual ESRBRating Rating { get; set; }

        /// <summary>
        /// The Game's minimum player count
        /// </summary>
        [Range(0, int.MaxValue)]
        [Required]
        public int MinimumPlayerCount { get; set; }

        /// <summary>
        /// The Game's maximum player count
        /// </summary>
        [CompareValues(nameof(MinimumPlayerCount), ComparisonCriteria.GreatThanOrEqualTo)]
        [Required]
        public int MaximumPlayerCount { get; set; }

        /// <summary>
        /// The URL for a trailer for the Game
        /// </summary>
        [Required]
        [DataType(DataType.Url)]
        public string TrailerURL { get; set; }

        /// <summary>
        /// The Game's short description
        /// </summary>
        [MaxLength(140)]
        [Required]
        public string ShortDescription { get; set; }

        /// <summary>
        /// The Game's long description
        /// </summary>
        public string LongDescription { get; set; }

        /// <summary>
        /// Collection navigation property for the Game's ESRB content descriptors
        /// </summary>
        public virtual ICollection<ESRBContentDescriptor> ContentDescriptors { get; set; }

        /// <summary>
        /// Collection navigation property for the Tags associated with the Game
        /// </summary>
        public virtual ICollection<Tag> GameCategories { get; set; }

        /// <summary>
        /// Collection navigation property for the product versions of the Game
        /// </summary>
        public virtual ICollection<GameProduct> GameSKUs { get; set; }
    }
}