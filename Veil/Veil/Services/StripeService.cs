using System;
using JetBrains.Annotations;
using Stripe;
using Veil.DataModels.Models;
using Veil.DataModels.Models.Identity;
using Veil.Services.Interfaces;

namespace Veil.Services
{
    [UsedImplicitly]
    public class StripeService : IStripeService
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
        /// <exception cref="StripeException">
        ///     Thrown if Stripe returns any errors
        /// </exception>
        public string CreateCustomer(User user)
        {
            var myCustomer = new StripeCustomerCreateOptions
            {
                Email = user.Email,
                Description = $"{user.FirstName} {user.LastName} ({user.Email})"
            };

            var customerService = new StripeCustomerService();

            StripeCustomer customer = customerService.Create(myCustomer);

            return customer.Id;
        }

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
        /// <exception cref="StripeException">
        ///     Thrown if Stripe returns any errors.
        ///     The messages are safe to display to the user.
        /// </exception>
        public MemberCreditCard CreateCreditCard(Member member, string stripeCardToken)
        {
            // Note: Stripe says their card_error messages are safe to display to the user
            //if (ex.StripeError.Code == "card_error")

            var newCard = new StripeCardCreateOptions
            {
                Source = new StripeSourceOptions
                {
                    TokenId = stripeCardToken
                    // Note: Adding Object = "card" will result in a generic stripe error
                }
            };

            var cardService = new StripeCardService();

            StripeCard card = cardService.Create(member.StripeCustomerId, newCard);

            int expiryMonth = int.Parse(card.ExpirationMonth);
            int expiryYear = int.Parse(card.ExpirationYear);

            MemberCreditCard newMemberCard = new MemberCreditCard
            {
                CardholderName = card.Name,
                ExpiryMonth = expiryMonth,
                ExpiryYear = expiryYear,
                Last4Digits = card.Last4,
                StripeCardId = card.Id,
                Member = member,
                MemberId = member.UserId
            };

            return newMemberCard;
        }

        /// <summary>
        ///     Retrived the last 4 digits for the credit card represented by the <see cref="stripeCardToken"/>
        /// </summary>
        /// <param name="stripeCardToken">
        ///     The token representing the card you want the last 4 digits of
        /// </param>
        /// <returns>
        ///     The last 4 digits of the token's card
        /// </returns>
        public string GetLast4ForToken(string stripeCardToken)
        {
            StripeTokenService tokenService = new StripeTokenService();

            StripeToken token = tokenService.Get(stripeCardToken);

            return token.StripeCard.Last4;
        }

        public string ChargeCard(decimal chargeAmount, string cardToken, string customerId = null)
        {
            var newCharge = new StripeChargeCreateOptions
            {
                Amount = (int)Math.Round(chargeAmount, 2) * 100,
                //Description = "",
                Capture = true,
                Currency = "CAD",
                CustomerId = customerId,
                StatementDescriptor = "Veil",
                Source = new StripeSourceOptions
                {
                    TokenId = cardToken
                }
            };

            StripeChargeService chargeService = new StripeChargeService();

            StripeCharge charge = chargeService.Create(newCharge);

            return charge.Id;
        }

        public bool RefundCharge(string chargeId)
        {
            StripeRefundService chargeService = new StripeRefundService();

            StripeRefund refund = chargeService.Create(chargeId);

            return refund.Amount > 0;
        }
    }
}