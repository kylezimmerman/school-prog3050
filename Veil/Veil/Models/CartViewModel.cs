using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Veil.DataModels.Models;

namespace Veil.Models
{
    public class CartViewModel
    {
        public Cart Cart { get; set; }
        public decimal ShippingCost { get; set; }
    }
}