using System.Collections.Generic;
using Veil.DataModels.Models;

namespace Veil.Models
{
    public class ManageCreditCardViewModel
    {
        public CreditCardViewModel creditCard;

        public IList<Country> Countries { get; set; }
    }
}