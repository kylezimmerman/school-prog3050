namespace Veil.Models
{
    public class CreditCardPaymentInformation
    {

        public int ExpiryMonth { get; set; }
        public int ExpiryYear { get; set; }

        public string CardSecurityCode { get; set; } 

        public string CardNumber { get; set; }
        public string NameOnCard { get; set; }
        public Address BillingAddress { get; set; }
    }
}