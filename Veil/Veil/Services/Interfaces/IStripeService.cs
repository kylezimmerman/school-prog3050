/* IStripeService.cs
 * Purpose: Interface for a service which interacts with Stripe
 * 
 * Revision History:
 *      Drew Matheson, 2015.10.27: Created
 */ 

using Veil.DataModels.Models;
using Veil.DataModels.Models.Identity;
using Veil.Exceptions;

namespace Veil.Services.Interfaces
{
    public interface IStripeService
    {
        /// <summary>
        ///     Creates a Stripe customer for the given user
        /// </summary>
        /// <param name="user">
        ///     The User to create a Stripe customer for
        /// </param>
        /// <returns>
        ///     The Stripe customer Id for the user
        /// </returns>
        /// <exception cref="StripeServiceException">
        ///     Thrown if Stripe returns any errors
        /// </exception>
        string CreateCustomer(User user);

        /// <summary>
        ///     Adds the credit card represented by <see cref="stripeCardToken"/> to the
        ///     <see cref="member"/>'s Customer account
        /// </summary>
        /// <param name="member">
        ///     The <see cref="Member"/> who this credit card belongs to
        /// </param>
        /// <param name="stripeCardToken">
        ///     The Stripe Token for the new credit card
        /// </param>
        /// <returns>
        ///     A new <see cref="MemberCreditCard"/> containing the new card's information
        /// </returns>
        /// <exception cref="StripeServiceException">
        ///     Thrown if Stripe returns any errors.
        ///     The messages are safe to display to the user.
        /// </exception>
        MemberCreditCard CreateCreditCard(Member member, string stripeCardToken);

        /// <summary>
        ///     Retrived the last 4 digits for the credit card represented by the <see cref="stripeCardToken"/>
        /// </summary>
        /// <param name="stripeCardToken">
        ///     The token representing the card you want the last 4 digits of
        /// </param>
        /// <returns>
        ///     The last 4 digits of the token's card
        /// </returns>
        /// <exception cref="StripeServiceException">
        ///     Thrown if Stripe returns any errors.
        /// </exception>
        string GetLast4ForToken(string stripeCardToken);

        /// <summary>
        ///     Charges the card represented by the <see cref="cardToken"/> for <see cref="chargeAmount"/>
        /// </summary>
        /// <param name="chargeAmount">
        ///     The amount (in dollars) to charge the card for
        /// </param>
        /// <param name="cardToken">
        ///     The Stripe Card Token to charge
        /// </param>
        /// <param name="customerId">
        ///     Optional. The customer Id of the customer being charged
        /// </param>
        /// <param name="description">
        ///     A description for the order
        /// </param>
        /// <returns>
        ///     The Stripe Charge Id for the charge.
        /// </returns>
        /// <exception cref="StripeServiceException">
        ///     Thrown if Stripe returns any errors.
        /// </exception>
        string ChargeCard(decimal chargeAmount, string cardToken, string customerId = null, string description = null);

        /// <summary>
        ///     Fully refunds the charge represented by <see cref="chargeId"/>
        /// </summary>
        /// <param name="chargeId">
        ///     The Stripe Charge Id to refund
        /// </param>
        /// <returns>
        ///     True if the amount refunded was greater than zero. False otherwise.
        ///     As we a refunding everything, if this returns true the refund succeeded.
        /// </returns>
        /// <exception cref="StripeServiceException">
        ///     Thrown if Stripe returns any errors.
        /// </exception>
        bool RefundCharge(string chargeId);
    }
}