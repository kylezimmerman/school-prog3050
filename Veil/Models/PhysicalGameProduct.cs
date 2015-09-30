namespace Veil.Models
{
    public class PhysicalGameProduct : GameProduct
    {
        public string SKUName { get; set; }

        public string NewSKU { get; set; }

        public string OldSKU { get; set; }

        public decimal UsedStorePrice { get; set; }

        public decimal UsedWebPrice { get; set; }

        public decimal MSRP { get; set; }

        public decimal NewStorePrice { get; set; }

        public decimal NewWebPrice { get; set; }

        public bool BuyBackUsedCopy { get; set; }
    }
}