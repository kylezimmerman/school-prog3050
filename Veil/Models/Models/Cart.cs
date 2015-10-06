/* Cart.cs
 * Purpose: A shopping cart for a specific Member
 * 
 * Revision History:
 *      Drew Matheson, 2015.10.02: Created
 */

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Veil.DataModels.Models
{
    // TODO: Do we care that we don't support carts for people who aren't logged in?
    /// <summary>
    /// A shopping cart containing the cart items for a specific member
    /// </summary>
    public class Cart
    {
        /// <summary>
        /// The Id for the Member whose cart this is
        /// </summary>
        [Key]
        public Guid MemberId { get; set; }

        /// <summary>
        /// Navigation property for the Member whose cart this is
        /// </summary>
        public virtual Member Member { get; set; }

        /// <summary>
        /// Collection navigation property for the items in the cart
        /// </summary>
        public virtual ICollection<CartItem> Items { get; set; }
    }
}