/* IShippingCostService.cs
 * Purpose: Interface for a service which calculates the shipping cost for cart items
 * 
 * Revision History:
 *      Drew Matheson, 2015.11.14: Created
 */ 

using System.Collections.Generic;
using Veil.DataModels.Models;

namespace Veil.Services.Interfaces
{
    /// <summary>
    ///     Interface containing a method for calculating the shipping cost for <see cref="CartItem"/>s
    /// </summary>
    public interface IShippingCostService
    {
        /// <summary>
        ///     Calculates the cost of shipping for the cart items
        /// </summary>
        /// <param name="orderSubtotal">
        ///     The subtotal for the order
        /// </param>
        /// <param name="items">
        ///     The <see cref="ICollection{T}"/> of <see cref="CartItem"/>s to calculate shipping cost of
        /// </param>
        /// <returns>
        ///     The calculated shipping cost
        /// </returns>
        decimal CalculateShippingCost(decimal orderSubtotal, ICollection<CartItem> items);
    }
}