/* Product.cs
 * Purpose: Abstract base class for all the product types we will be dealing with
 * 
 * Revision History:
 *      Drew Matheson, 2015.10.02: Created
 */

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Veil.DataModels.Models
{
    /// <summary>
    /// Enumeration of the availability status for products
    /// </summary>
    public enum AvailabilityStatus
    {
        /// <summary>
        /// The product is available for pre-order
        /// </summary>
        [Description("Pre-order")]
        PreOrder,

        /// <summary>
        /// The product is available for purchase
        /// </summary>
        Available,

        /// <summary>
        /// The product has been discontinued by the manufacturer
        /// </summary>
        DiscontinuedByManufacturer,

        /// <summary>
        /// We are no longer selling the product
        /// </summary>
        NotForSale
    }

    /// <summary>
    /// Abstract base class for all products
    /// </summary>
    public abstract class Product
    {
        /// <summary>
        /// The Id for this product
        /// </summary>
        [Key]
        public Guid Id { get; set; }

        /// <summary>
        /// The name of this product
        /// </summary>
        public abstract string Name { get; }

        /// <summary>
        /// The Game's availability status
        /// </summary>
        public AvailabilityStatus ProductAvailabilityStatus { get; set; }

        /// <summary>
        /// The release date of this product
        /// </summary>
        public DateTime ReleaseDate { get; set; }

        /// <summary>
        /// The web price for a new version of this product.
        /// </summary>
        [DataType(DataType.Currency)]
        public decimal NewWebPrice { get; set; }

        /// <summary>
        /// The web price for a pre-used version of this product.
        /// null if the product doesn't have a pre-used version.
        /// </summary>
        [DataType(DataType.Currency)]
        public decimal? UsedWebPrice { get; set; }

        /// <summary>
        /// The URL for the box art image for this product
        /// </summary>
        [DataType(DataType.ImageUrl)]
        [Url]
        public string BoxArtImageURL { get; set; }

        /// <summary>
        /// A description for this specific sku of a product. 
        /// This should only contain information that is specific to the SKU
        /// </summary>
        [MaxLength(1024)]
        public string SKUDescription { get; set; }

        /// <summary>
        /// Collection navigation property for this Product's tags
        /// </summary>
        public virtual ICollection<Tag> Tags { get; set; }

        /// <summary>
        /// Collection navigation property for this Product's invetory level at locations
        /// </summary>
        public virtual ICollection<ProductLocationInventory> LocationInventories { get; set; }
    }
}