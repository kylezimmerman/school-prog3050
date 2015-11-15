using System.Collections.Generic;
using Veil.DataModels.Models;

namespace Veil.Services.Interfaces
{
    public interface IShippingCostService
    {
        decimal CalculateShippingCost(decimal orderSubtotal, ICollection<CartItem> items);
    }
}
