/* CartItem.cs
 * Purpose: A class for items in an order
 * 
 * Revision History:
 *      Drew Matheson, 2015.10.02: Created
 */ 

using System;
using System.ComponentModel.DataAnnotations;

namespace Veil.Models
{
    /// <summary>
    /// An item in an order. Includes product quantity and the order it belongs to
    /// </summary>
    public class OrderItem
    {
        /// <summary>
        /// The Id of the order this item is part of.
        /// </summary>
        [Key]
        public Guid OrderId { get; set; }

        /// <summary>
        /// The Id of the product this OrderItem is for
        /// </summary>
        [Key]
        public Guid ProductId { get; set; }

        /// <summary>
        /// Navigation property for the Product this OrderItem is for
        /// </summary>
        public virtual Product Product { get; set; }

        /// <summary>
        /// Flag indicating if the product is a new or used product
        /// </summary>
        public bool IsNew { get; set; }

        /// <summary>
        /// Quantity of the product for this OrderItem
        /// </summary>
        public int Quantity { get; set; }
    }
}