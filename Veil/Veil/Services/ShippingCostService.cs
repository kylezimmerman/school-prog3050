using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Veil.DataModels.Models;
using Veil.Services.Interfaces;

namespace Veil.Services
{
    [UsedImplicitly]
    public class ShippingCostService : IShippingCostService {
        public decimal CalculateShippingCost(decimal orderSubtotal, ICollection<CartItem> items)
        {
            return Math.Round(orderSubtotal < 120m ? 12.00m : 0.00m, 2);
        }
    }
}