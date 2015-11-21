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
        // TODO: I'd like to create our own StripeException and handle the exceptions in here in a way that lets the users just output the message of the exception

        /// <summary>
        ///     Implements <see cref="IStripeService.CreateCustomer"/>
        /// </summary>
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
        ///     Implements <see cref="IStripeService.CreateCreditCard"/>
        /// </summary>
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
        ///     Implements <see cref="IStripeService.GetLast4ForToken"/>
        /// </summary>
        public string GetLast4ForToken(string stripeCardToken)
        {
            StripeTokenService tokenService = new StripeTokenService();

            StripeToken token = tokenService.Get(stripeCardToken);

            return token.StripeCard.Last4;
        }

        /// <summary>
        ///     Implements <see cref="IStripeService.ChargeCard"/>
        /// </summary>
        public string ChargeCard(decimal chargeAmount, string cardToken, string customerId = null, string description = null)
        {
            var newCharge = new StripeChargeCreateOptions
            {
                Amount = (int)Math.Round(chargeAmount, 2) * 100,
                Description = description,
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

        /// <summary>
        ///     Implements <see cref="IStripeService.RefundCharge"/>
        /// </summary>
        public bool RefundCharge(string chargeId)
        {
            StripeRefundService refundService = new StripeRefundService();

            StripeRefund refund = refundService.Create(chargeId);

            return refund.Amount > 0;
        }
    }
}