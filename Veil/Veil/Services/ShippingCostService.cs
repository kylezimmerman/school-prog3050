/* ShippingCostService.cs
 * Purpose: Simple implementation of IShippingCostService with static costs
 * 
 * Revision History:
 *      Drew Matheson, 2015.11.14: Created
 */ 

using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Veil.DataModels.Models;
using Veil.Services.Interfaces;

namespace Veil.Services
{
    /// <summary>
    ///     Simple implementation of <see cref="IShippingCostService"/> with static costs
    /// </summary>
    [UsedImplicitly]
    public class ShippingCostService : IShippingCostService
    {
        /// <summary>
        ///     Implements <see cref="IShippingCostService.CalculateShippingCost(decimal, ICollection{CartItem})"/>
        /// </summary>
        public decimal CalculateShippingCost(decimal orderSubtotal, ICollection<CartItem> items)
        {
            return Math.Round(orderSubtotal < 120m ? 12.00m : 0.00m, 2);
        }
    }
}