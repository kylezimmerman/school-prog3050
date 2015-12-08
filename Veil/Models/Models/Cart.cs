/* Cart.cs
 * Purpose: A shopping cart for a specific Member
 * 
 * Revision History:
 *      Drew Matheson, 2015.10.02: Created
 */

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Veil.DataModels.Models
{
    /// <summary>
    ///     A shopping cart containing the cart items for a specific member
    /// </summary>
    public class Cart
    {
        /// <summary>
        ///     The Id for the Member whose cart this is
        /// </summary>
        [Key]
        public Guid MemberId { get; set; }

        /// <summary>
        ///     Navigation property for the Member whose cart this is
        /// </summary>
        public virtual Member Member { get; set; }

        /// <summary>
        ///     Collection navigation property for the items in the cart
        /// </summary>
        public virtual ICollection<CartItem> Items { get; set; }

        /// <summary>
        ///     Gets the total item price for all of the items in the cart
        /// </summary>
        public decimal TotalCartItemsPrice
        {
            get
            {
                return
                    Items.Where(i => i.IsNew).Sum(i => i.Product.NewWebPrice * i.Quantity) +
                    Items.Where(i => !i.IsNew).Sum(i => i.Product.UsedWebPrice * i.Quantity) ?? 0m;
            }
        }
    }
}