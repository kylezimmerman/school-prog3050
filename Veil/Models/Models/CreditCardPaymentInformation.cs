using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Veil.DataModels.Models
{
    // TODO: Figure out what this should actually be
    // TODO: Figure out how member links to this (Unsure with composite key)
    public class CreditCardPaymentInformation
    {
        [Key, Column(Order = 0), ForeignKey(nameof(Member))]
        public Guid MemberId { get; set; }

        public virtual Member Member { get; set; }

        public int ExpiryMonth { get; set; }
        public int ExpiryYear { get; set; }

        public string CardSecurityCode { get; set; } 

        [Key, Column(Order = 1)]
        public string CardNumber { get; set; }

        public string NameOnCard { get; set; }
    }
}