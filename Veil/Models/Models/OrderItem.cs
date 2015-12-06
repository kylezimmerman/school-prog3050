/* CartItem.cs
 * Purpose: A class for items in an order
 * 
 * Revision History:
 *      Drew Matheson, 2015.10.02: Created
 */

using System;
using System.ComponentModel.DataAnnotations;

namespace Veil.DataModels.Models
{
    /// <summary>
    ///     An item in an order. Includes product quantity and the order it belongs to
    /// </summary>
    public class OrderItem
    {
        /// <summary>
        ///     The Id of the order this item is part of.
        /// </summary>
        [Key]
        public long OrderId { get; set; }

        /// <summary>
        ///     The Id of the product this OrderItem is for
        /// </summary>
        [Key]
        public Guid ProductId { get; set; }

        /// <summary>
        ///     Navigation property for the Product this OrderItem is for
        /// </summary>
        public virtual Product Product { get; set; }

        /// <summary>
        ///     Flag indicating if the product is a new or used product
        /// </summary>
        [Key]
        [Required]
        public bool IsNew { get; set; }

        /// <summary>
        ///     Quantity of the product for this OrderItem
        /// </summary>
        [Required]
        public int Quantity { get; set; }

        /// <summary>
        ///     The price the product was sold for
        /// </summary>
        [Required]
        public decimal ListPrice { get; set; }
    }
}