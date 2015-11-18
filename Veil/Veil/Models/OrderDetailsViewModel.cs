using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Veil.DataModels.Models;

namespace Veil.Models
{
    public class OrderDetailsViewModel
    {
        public WebOrder Order { get; set; }
        public decimal ItemTotal { get; set; }

    }
}