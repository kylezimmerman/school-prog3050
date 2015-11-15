using Veil.DataModels.Models;
using Veil.DataModels.Models.Identity;

namespace Veil.Services.Interfaces
{
    public interface IStripeService
    {
        string CreateCustomer(User user);

        MemberCreditCard CreateCreditCard(Member member, string stripeCardToken);

        string GetLast4ForToken(string stripeCardToken);

        string ChargeCard(decimal chargeAmount, string cardToken, string customerId = null);

        bool RefundCharge(string chargeId);
    }
}
