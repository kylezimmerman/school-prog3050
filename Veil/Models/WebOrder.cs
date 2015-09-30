using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Veil.Models
{
    public class WebOrder
    {
        [Key]
        public string Id { get; set; }

        [Required]
        public string MemberId { get; set; }

        [ForeignKey(nameof(MemberId))]
        public Member Member { get; set; }

        public CreditCardPaymentInformation CreditCardPaymentInformation { get; set; }

        public Address ShippingAddress { get; set; }

        public virtual ICollection<CartItem> OrderItems { get; set; }
    }
}