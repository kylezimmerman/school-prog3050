using System.Collections.Generic;
using System.Linq;
using Veil.DataModels.Models;

namespace Veil.Models.Reports
{
    public class SalesViewModel
    {
        public DateFilteredViewModel DateFilter { get; set; }
        public IEnumerable<SalesOrderViewModel> Orders { get; set; }
        public int OrderCount => Orders.Count();
        public int TotalQuantity => Orders.Sum(o => o.Quantity);
        public decimal ItemsSum => Orders.Sum(o => o.Subtotal);
        public decimal ShippingSum => Orders.Sum(o => o.Shipping);
        public decimal TaxSum => Orders.Sum(o => o.Tax);
        public decimal Total => Orders.Sum(o => o.OrderTotal);
    }

    public class SalesOrderViewModel
    {
        public long OrderNumber { get; set; }
        public string Username { get; set; }
        public int Quantity { get; set; }
        public decimal Subtotal { get; set; }
        public decimal Shipping { get; set; }
        public decimal Tax { get; set; }
        public decimal OrderTotal { get; set; }
    }
}