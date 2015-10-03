namespace Veil.Models
{
    public class PhysicalGameProduct : GameProduct
    {
        public override string Name => $"{base.Name} {SKUNameSuffix}"; // TODO: Maybe this isn't the best way to do this?

        /// <summary>
        /// The optional suffix for this specific SKU of the game.
        /// <example>
        ///     Collector's Edition
        /// </example>
        /// </summary>
        public string SKUNameSuffix { get; set; }

        public string NewSKU { get; set; } // TODO: This needs to be unique

        public string UsedSKU { get; set; } // TODO: This needs to be unique

        // TODO: Do we actually need this info? Why is the price different in-store vs web
        /// <summary>
        /// The in-store price for a pre-used version of this product.
        /// null if the product doesn't have a pre-used version.
        /// </summary>
        public decimal? UsedStorePrice { get; set; }

        /// <summary>
        /// The in-store price for a new version of this product.
        /// </summary>
        public decimal NewStorePrice { get; set; }

        /// <summary>
        /// Flag which indicates if we will buy pre-used copies from customers
        /// </summary>
        public bool WillBuyBackUsedCopy { get; set; }
    }
}