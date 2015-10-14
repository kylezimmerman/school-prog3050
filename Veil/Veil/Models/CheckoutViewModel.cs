using System.Collections.Generic;
using Veil.DataModels.Models;

namespace Veil.Models
{
    public class CheckoutViewModel
    {
        public virtual MemberAddress Address { get; set; }

        public virtual MemberCreditCard BillingInfo { get; set; }

        public virtual ICollection<CartItem> Items { get; set; }
    }
}