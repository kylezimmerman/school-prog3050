/* StripeService.cs
 * Purpose: Implementation of IStripeService for interacting with Stripe using Stripe.net
 * 
 * Revision History:
 *      Drew Matheson, 2015.10.27: Created
 */ 

using System;
using System.Diagnostics;
using System.Net;
using JetBrains.Annotations;
using Stripe;
using Veil.DataModels.Models;
using Veil.DataModels.Models.Identity;
using Veil.Exceptions;
using Veil.Services.Interfaces;

namespace Veil.Services
{
    /// <summary>
    ///     Implementation of IStripeService using Stripe.net
    /// </summary>
    [UsedImplicitly]
    public class StripeService : IStripeService
    {
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

            try
            {
                StripeCustomer customer = customerService.Create(myCustomer);
                return customer.Id;
            }
            catch (StripeException ex)
            {
                string exceptionMessage;
                StripeExceptionType type;

                if (ex.HttpStatusCode >= HttpStatusCode.InternalServerError
                    || (int)ex.HttpStatusCode == 429 /* Too Many Requests */
                    || (int)ex.HttpStatusCode == 402 /* Request Failed */)
                {
                    type = StripeExceptionType.ServiceError;
                    exceptionMessage = 
                        "An error occured while creating a customer account for you. Please try again later.";
                }
                else if (ex.HttpStatusCode == HttpStatusCode.Unauthorized)
                {
                    // Note: We want to log this as it means we don't have a valid API key
                    Debug.WriteLine("Stripe API Key is Invalid");
                    Debug.WriteLine(ex.Message);

                    type = StripeExceptionType.ApiKeyError;
                    exceptionMessage = "An error occured while talking to one of our backends. Sorry!";
                }
                else
                {
                    // Note: Log unknown errors
                    Debug.WriteLine(ex.HttpStatusCode);
                    Debug.WriteLine($"Stripe Type: {ex.StripeError.ErrorType}");
                    Debug.WriteLine($"Stripe Message: {ex.StripeError.Message}");
                    Debug.WriteLine($"Stripe Code: {ex.StripeError.Code}");
                    Debug.WriteLine($"Stripe Param: {ex.StripeError.Parameter}");

                    type = StripeExceptionType.UnknownError;
                    exceptionMessage = 
                        "An unknown error occured while creating a customer account for you. Please try again later.";
                }

                throw new StripeServiceException(exceptionMessage, type, ex);
            }
        }

        /// <summary>
        ///     Implements <see cref="IStripeService.CreateCreditCard"/>
        /// </summary>
        public MemberCreditCard CreateCreditCard(Member member, string stripeCardToken)
        {
            var newCard = new StripeCardCreateOptions
            {
                Source = new StripeSourceOptions
                {
                    TokenId = stripeCardToken
                    // Note: Adding Object = "card" will result in a generic stripe error
                }
            };

            var cardService = new StripeCardService();

            StripeCard card;

            try
            {
                card = cardService.Create(member.StripeCustomerId, newCard);
            }
            catch (StripeException ex)
            {
                throw HandleStripeException(ex);
            }

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

            try
            {
                StripeToken token = tokenService.Get(stripeCardToken);

                return token.StripeCard.Last4;
            }
            catch (StripeException ex)
            {
                throw HandleStripeException(ex);
            }
            
        }

        /// <summary>
        ///     Implements <see cref="IStripeService.ChargeCard"/>
        /// </summary>
        public string ChargeCard(
            decimal chargeAmount, string cardToken, string customerId = null, string description = null)
        {
            var newCharge = new StripeChargeCreateOptions
            {
                Amount = (int) Math.Round(chargeAmount, 2) * 100,
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

            try
            {
                StripeCharge charge = chargeService.Create(newCharge);

                return charge.Id;
            }
            catch (StripeException ex)
            {
                throw HandleStripeException(ex);
            }
            
        }

        /// <summary>
        ///     Implements <see cref="IStripeService.RefundCharge"/>
        /// </summary>
        public bool RefundCharge(string chargeId)
        {
            StripeRefundService refundService = new StripeRefundService();

            try
            {
                StripeRefund refund = refundService.Create(chargeId);

                return refund.Amount > 0;
            }
            catch (StripeException ex)
            {
                string message;
                StripeExceptionType type;

                if (ex.StripeError.ErrorType == "card_error")
                {
                    message = $"{ex.Message}. This occured while refunding payment. " + 
                        "Please contact customer support.";
                    type = StripeExceptionType.CardError;
                }
                else if (ex.HttpStatusCode == HttpStatusCode.Unauthorized)
                {
                    // Note: We want to log this as it means we don't have a valid API key
                    Debug.WriteLine("Stripe API Key is Invalid");
                    Debug.WriteLine(ex.Message);

                    type = StripeExceptionType.ApiKeyError;
                    message = "An error occurred while refunding payment. " +
                        "Please contact customer support.";
                }
                else
                {
                    type = StripeExceptionType.UnknownError;
                    message = "An error occurred while refunding payment. " +
                        "Please contact customer support.";
                }

                throw new StripeServiceException(message, type, ex);
            }
        }

        /// <summary>
        ///     Converts the <see cref="StripeException"/> into a non-Stripe.net exception
        /// </summary>
        /// <param name="ex">
        ///     The <see cref="StripeException"/> to convert
        /// </param>
        /// <returns>
        ///     A <see cref="StripeServiceException"/> representing the original 
        ///     <see cref="StripeException"/>
        /// </returns>
        protected virtual StripeServiceException HandleStripeException(StripeException ex)
        {
            string message;
            StripeExceptionType type;

            // Note: Stripe says their card_error messages are safe to display to the user
            if (ex.StripeError.ErrorType == "card_error")
            {
                message = ex.Message;
                type = StripeExceptionType.CardError;
            }
            else if (ex.HttpStatusCode == HttpStatusCode.Unauthorized)
            {
                // Note: We want to log this as it means we don't have a valid API key
                Debug.WriteLine("Stripe API Key is Invalid");
                Debug.WriteLine(ex.Message);

                type = StripeExceptionType.ApiKeyError;
                message = "An error occured while talking to one of our backends. Sorry!";
            }
            else
            {
                type = StripeExceptionType.UnknownError;
                message =
                    "An error occured while talking to one of our backends. Sorry!";
            }

            return new StripeServiceException(message, type, ex);
        }
    }
}