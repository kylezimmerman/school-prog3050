using Veil.DataModels.Models;
using Veil.DataModels.Models.Identity;

namespace Veil.Services.Interfaces
{
    public interface IStripeService
    {
        string CreateCustomer(User user);

        string CreateCreditCard(Member member, CreditCardViewModel viewModel);
    }
}
