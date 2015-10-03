/* Product.cs
 * Purpose: Abstract base class for all the product types we will be dealing with
 * 
 * Revision History:
 *      Drew Matheson, 2015.10.02: Created
 */ 

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Veil.Models
{
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
        /// The release date of this product
        /// </summary>
        public DateTime ReleaseDate { get; set; }

        // TODO: Do we need this? Only thing I can think of is to show our prices are better
        /// <summary>
        /// The manufacturers suggested retail price for this product
        /// </summary>
        public decimal MSRP { get; set; }

        /// <summary>
        /// The web price for a new version of this product.
        /// </summary>
        public decimal NewWebPrice { get; set; }

        /// <summary>
        /// The web price for a pre-used version of this product.
        /// null if the product doesn't have a pre-used version.
        /// </summary>
        public decimal? UsedWebPrice { get; set; }

        /// <summary>
        /// Collection navigation property for this Product's tags
        /// </summary>
        public virtual ICollection<Tag> Tags { get; set; }
    }
}