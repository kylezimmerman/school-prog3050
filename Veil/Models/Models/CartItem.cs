/* CartItem.cs
 * Purpose: A class for products in a cart
 * 
 * Revision History:
 *      Drew Matheson, 2015.10.02: Created
 */

using System;
using System.ComponentModel.DataAnnotations;

namespace Veil.DataModels.Models
{
    /// <summary>
    /// An item in a cart. Includes product quantity and the cart it belongs to
    /// </summary>
    public class CartItem
    {
        /// <summary>
        /// The Id of the member this cart is for.
        /// As a member can only have one cart, this also acts as the primary key for the cart
        /// </summary>
        [Key]
        public Guid MemberId { get; set; }

        /// <summary>
        /// The Id of the product this CartItem is for
        /// </summary>
        [Key]
        public Guid ProductId { get; set; }

        /// <summary>
        /// Navigation property for the Product this CartItem is for
        /// </summary>
        public virtual Product Product { get; set; }

        /// <summary>
        /// Flag indicating if the product is a new or used product
        /// </summary>
        public bool IsNew { get; set; }

        /// <summary>
        /// Quantity of the product for this CartItem
        /// </summary>
        public int Quantity { get; set; }
    }
}