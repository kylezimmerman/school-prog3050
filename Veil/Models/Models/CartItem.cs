/* CartItem.cs
 * Purpose: A class for products in a cart
 * 
 * Revision History:
 *      Drew Matheson, 2015.10.02: Created
 */

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Veil.DataModels.Models
{
    /// <summary>
    ///     An item in a cart. Includes product quantity and the cart it belongs to
    /// </summary>
    public class CartItem
    {
        /// <summary>
        ///     The Id of the member this cart is for.
        ///     As a member can only have one cart, this also acts as the primary key for the cart
        /// </summary>
        [Key]
        public Guid MemberId { get; set; }

        /// <summary>
        ///     The Id of the product this CartItem is for
        /// </summary>
        [Key]
        public Guid ProductId { get; set; }

        /// <summary>
        ///     Navigation property for the Product this CartItem is for
        /// </summary>
        public virtual Product Product { get; set; }

        /// <summary>
        ///     Flag indicating if the product is a new or used product
        /// </summary>
        [Key]
        public bool IsNew { get; set; }

        /// <summary>
        ///     Quantity of the product for this CartItem
        /// </summary>
        public int Quantity { get; set; }

        /// <summary>
        ///     Implementation of <see cref="IEqualityComparer{T}"/> for <see cref="CartItem"/>
        /// </summary>
        private sealed class CartItemEqualityComparer : IEqualityComparer<CartItem>
        {
            /// <summary>
            ///     Implements <see cref="IEqualityComparer{T}.Equals(T, T)"/>.
            ///     Compares whether the two <see cref="CartItem"/>s are equal
            /// </summary>
            /// <param name="x">
            ///     The first <see cref="CartItem"/>
            /// </param>
            /// <param name="y">
            ///     The second <see cref="CartItem"/>
            /// </param>
            /// <returns>
            ///     True if the two <see cref="CartItem"/>s are considered equal.
            ///     False otherwise
            /// </returns>
            public bool Equals(CartItem x, CartItem y)
            {
                if (ReferenceEquals(x, y))
                {
                    return true;
                }
                if (ReferenceEquals(x, null))
                {
                    return false;
                }
                if (ReferenceEquals(y, null))
                {
                    return false;
                }
                return x.MemberId.Equals(y.MemberId) &&
                    x.ProductId.Equals(y.ProductId) &&
                    x.IsNew == y.IsNew &&
                    x.Quantity == y.Quantity;
            }

            /// <summary>
            ///     Implements <see cref="IEqualityComparer{T}.GetHashCode(T)"/>.
            /// </summary>
            /// <param name="obj">
            ///     The <see cref="CartItem"/> to get the hash code of.
            /// </param>
            /// <returns>
            ///     <see cref="obj"/>'s hashcode
            /// </returns>
            /// <remarks>
            ///     The hashcode is generated using <see cref="MemberId"/>, <see cref="ProductId"/>, 
            ///     <see cref="IsNew"/>, and <see cref="Quantity"/>
            /// </remarks>
            public int GetHashCode(CartItem obj)
            {
                unchecked
                {
                    var hashCode = obj.MemberId.GetHashCode();
                    hashCode = (hashCode * 397) ^ obj.ProductId.GetHashCode();
                    hashCode = (hashCode * 397) ^ obj.IsNew.GetHashCode();
                    hashCode = (hashCode * 397) ^ obj.Quantity.GetHashCode();
                    return hashCode;
                }
            }
        }

        // Singleton instance of <see cref="CartItemEqualityComparer"/>
        private static readonly IEqualityComparer<CartItem> CartItemComparerInstance = new CartItemEqualityComparer();

        /// <summary>
        ///     Gets an instance of <see cref="IEqualityComparer{T}"/> for <see cref="CartItem"/>
        /// </summary>
        public static IEqualityComparer<CartItem> CartItemComparer => CartItemComparerInstance;
    }
}