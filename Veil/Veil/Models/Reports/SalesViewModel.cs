using System.Linq;

namespace Veil.Models.Reports
{
    public class SalesViewModel : DateFilteredListViewModel<SalesOrderViewModel>
    {
        public int OrderCount => Items.Count();
        public int TotalQuantity => Items.Sum(o => o.Quantity);
        public decimal ItemsSum => Items.Sum(o => o.Subtotal);
        public decimal ShippingSum => Items.Sum(o => o.Shipping);
        public decimal TaxSum => Items.Sum(o => o.Tax);
        public decimal Total => Items.Sum(o => o.OrderTotal);
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