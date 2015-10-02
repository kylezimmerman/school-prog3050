using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Veil.Models
{
    public class CreditCardPaymentInformation
    {
        [Key, ForeignKey(nameof(Member))]
        public Guid MemberId { get; set; }

        public Member Member { get; set; }

        public int ExpiryMonth { get; set; }
        public int ExpiryYear { get; set; }

        public string CardSecurityCode { get; set; } 

        [Key]
        public string CardNumber { get; set; }
        public string NameOnCard { get; set; }
        public Address BillingAddress { get; set; }
    }
}