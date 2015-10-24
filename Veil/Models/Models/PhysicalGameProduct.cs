/* PhysicalGameProduct.cs
 * Purpose: A class for a physical version of a GameProduct (i.e. a boxed game)
 * 
 * Revision History:
 *      Drew Matheson, 2015.10.03: Created
 */

using System.ComponentModel.DataAnnotations;

namespace Veil.DataModels.Models
{
    /// <summary>
    /// A physical product version of a GameProduct
    /// </summary>
    public class PhysicalGameProduct : GameProduct
    {
        public override string Name => $"{base.Name} {SKUNameSuffix}";

        /// <summary>
        /// The optional suffix for this specific SKU of the game.
        /// <example>
        ///     Collector's Edition
        /// </example>
        /// </summary>
        [MaxLength(256)]
        public string SKUNameSuffix { get; set; }

        /// <summary>
        /// The internal SKU number for a new version of this product
        /// </summary>
        [MaxLength(128)]
        public string InternalNewSKU { get; set; }

        /// <summary>
        /// The internal SKU number for a used version of this product
        /// </summary>
        [MaxLength(128)]
        public string InteralUsedSKU { get; set; }

        /// <summary>
        /// Flag which indicates if we will buy pre-used copies from customers
        /// </summary>
        public bool WillBuyBackUsedCopy { get; set; }
    }
}