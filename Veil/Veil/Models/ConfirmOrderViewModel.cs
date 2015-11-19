using System;
using System.Collections.Generic;
using Veil.DataModels.Models;

namespace Veil.Models
{
    public class ConfirmOrderViewModel
    {
        public string FullName { get; set; }

        public Address Address { get; set; }

        public string ProvinceName { get; set; }

        public string CountryName { get; set; }

        public string CreditCardLast4Digits { get; set; }

        public virtual ICollection<ConfirmOrderCartItemViewModel> Items { get; set; }

        public decimal ItemSubTotal { get; set; }
        
        public decimal TaxAmount { get; set; }

        public decimal ShippingCost { get; set; }
        public string PhoneNumber { get; set; }
    }

    public class ConfirmOrderCartItemViewModel
    {
        public Guid ProductId { get; set; }

        public string Name { get; set; }

        public string PlatformName { get; set; }

        public bool IsNew { get; set; }

        public int Quantity { get; set; }

        public decimal ItemPrice { get; set; }

        public decimal ItemTotal => Quantity * ItemPrice;
    }
}