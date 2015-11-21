/* PhysicalGameProduct.cs
 * Purpose: A class for a physical version of a GameProduct (i.e. a boxed game)
 * 
 * Revision History:
 *      Drew Matheson, 2015.10.03: Created
 */

using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Veil.DataModels.Validation;

namespace Veil.DataModels.Models
{
    /// <summary>
    /// A physical product version of a GameProduct
    /// </summary>
    public class PhysicalGameProduct : GameProduct
    {
        public override string Name => $"{base.Name} {SKUNameSuffix}";

        public override string NamePlatformDistinct => $"{Name} ({Platform.PlatformName})";

        /// <summary>
        /// The optional suffix for this specific SKU of the game.
        /// <example>
        ///     Collector's Edition
        /// </example>
        /// </summary>
        [MaxLength(255)]
        [DisplayName("SKU Suffix")]
        public string SKUNameSuffix { get; set; }

        /// <summary>
        ///     The internal SKU number for a new version of this product
        /// </summary>
        /// <remarks>
        ///     This is a 13 digit string with the first digit being a 0
        ///     <example>
        ///         0000000000123
        ///         0200300400500
        ///     </example>
        /// </remarks>
        [MaxLength(128)]
        [RegularExpression(ValidationRegex.PHYSICAL_GAME_PRODUCT_NEW_SKU)]
        [DisplayName("Internal New SKU")]
        public string InternalNewSKU { get; set; }

        /// <summary>
        ///     The internal SKU number for a used version of this product
        /// </summary>
        /// <remarks>
        ///     This is a 13 digit string with the first digit being a 1
        ///     <example>
        ///         1000000000123
        ///         1200300400500
        ///     </example>
        /// </remarks>
        [MaxLength(128)]
        [RegularExpression(ValidationRegex.PHYSICAL_GAME_PRODUCT_USED_SKU)]
        [DisplayName("Internal Used SKU")]
        public string InteralUsedSKU { get; set; }

        /// <summary>
        /// Flag which indicates if we will buy pre-used copies from customers
        /// </summary>
        [DisplayName("Will Buy Used")]
        public bool WillBuyBackUsedCopy { get; set; }
    }
}