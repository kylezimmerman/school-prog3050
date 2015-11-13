using Veil.DataModels.Models;
using Veil.DataModels.Models.Identity;
using Veil.Models;

namespace Veil.Services.Interfaces
{
    public interface IStripeService
    {
        string CreateCustomer(User user);

        MemberCreditCard CreateCreditCard(Member member, string stripeCardToken);
    }
}
