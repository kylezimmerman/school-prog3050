﻿/* Game.cs
 * Purpose: A class for information about a game that isn't specific to a product version of it
 * 
 * Revision History:
 *      Drew Matheson, 2015.10.02: Created
 */

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Veil.DataModels.Validation;

namespace Veil.DataModels.Models
{
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
        [StringLength(maximumLength: 512, MinimumLength = 1)]
        public string Name { get; set; }

        /// <summary>
        /// The Game's availability status
        /// </summary>
        [DisplayName("Game Availability")]
        public AvailabilityStatus GameAvailabilityStatus
        {
            get
            {
                if (GameSKUs == null || GameSKUs.Count == 0)
                {
                    return AvailabilityStatus.NotForSale;
                }

                if (GameSKUs.Any(gp => gp.ProductAvailabilityStatus == AvailabilityStatus.PreOrder)) {
                    return AvailabilityStatus.PreOrder;
                }

                if (GameSKUs.Any(gp => gp.ProductAvailabilityStatus == AvailabilityStatus.Available))
                {
                    return AvailabilityStatus.Available;
                }

                if (GameSKUs.Any(
                        gp => gp.ProductAvailabilityStatus == AvailabilityStatus.DiscontinuedByManufacturer))
                {
                    return AvailabilityStatus.DiscontinuedByManufacturer;
                }

                return AvailabilityStatus.NotForSale;
            }
        }

        /// <summary>
        ///     The Game's SKU's average rating
        /// </summary>
        public double? AverageRating
        {
            get
            {
                return GameSKUs.SelectMany(g => g.Reviews).Average(r => (double?) r.Rating);
            }
        }

        /// <summary>
        /// The Id for the Game's ESRB rating
        /// </summary>
        [Required]
        [DisplayName("ESRB Rating")]
        public string ESRBRatingId { get; set; }

        /// <summary>
        /// Navigation property for the Game's ESRB rating
        /// </summary>
        [DisplayName("ESRB Rating")]
        public virtual ESRBRating Rating { get; set; }

        /// <summary>
        /// The Game's minimum player count
        /// </summary>
        [Range(1, int.MaxValue)]
        [Required]
        [DisplayName("Minimum Player Count")]
        public int MinimumPlayerCount { get; set; }

        /// <summary>
        /// The Game's maximum player count
        /// </summary>
        [CompareValues(nameof(MinimumPlayerCount), ComparisonCriteria.GreatThanOrEqualTo)]
        [Range(1, int.MaxValue)]
        [Required]
        [DisplayName("Maximum Player Count")]
        public int MaximumPlayerCount { get; set; }

        /// <summary>
        /// The URL for a trailer for the Game
        /// </summary>
        [DataType(DataType.Url)]
        [Url]
        [MaxLength(2048)]
        [DisplayName("Trailer URL")]
        public string TrailerURL { get; set; }

        /// <summary>
        /// The Game's short description
        /// </summary>
        [MaxLength(140)]
        [Required]
        [DisplayName("Short Description")]
        public string ShortDescription { get; set; }

        /// <summary>
        /// The Game's long description
        /// </summary>
        [MaxLength(2048)]
        [DataType(DataType.MultilineText)]
        [DisplayName("Long Description")]
        public string LongDescription { get; set; }

        /// <summary>
        /// The URL for the primary image for this game
        /// </summary>
        [DataType(DataType.Url)]
        [MaxLength(2048)]
        [DisplayName("Primary Image URL")]
        public string PrimaryImageURL { get; set; }

        /// <summary>
        /// Collection navigation property for this Game's tags
        /// </summary>
        public virtual ICollection<Tag> Tags { get; set; }

        /// <summary>
        /// Collection navigation property for the Game's ESRB content descriptors
        /// </summary>
        public virtual ICollection<ESRBContentDescriptor> ContentDescriptors { get; set; }

        /// <summary>
        /// Collection navigation property for the product versions of the Game
        /// </summary>
        public virtual ICollection<GameProduct> GameSKUs { get; set; }
    }
}