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

        public string CreateCreditCard(Member member, CreditCardViewModel creditCard)
        {
            // Note: Stripe says their card_error messages are safe to display to the user
            //if (ex.StripeError.Code == "card_error")

            var newCard = new StripeCardCreateOptions
            {
                Source = new StripeSourceOptions
                {
                    ExpirationYear = creditCard.ExpirationYear.ToString(),
                    ExpirationMonth = creditCard.ExpirationMonth.ToString(),
                    AddressCountry = creditCard.AddressCountry, // optional
                    AddressLine1 = creditCard.AddressLine1, // optional
                    //AddressLine2 = "Apt 24", // optional
                    AddressCity = creditCard.AddressCity, // optional
                    AddressState = creditCard.AddressState, // optional
                    AddressZip = creditCard.AddressZip, // optional
                    Name = creditCard.Name, // optional
                    Cvc = creditCard.Cvc, // optional
                    Object = creditCard.Object
                }
            };

            var cardService = new StripeCardService();

            StripeCard card = cardService.Create(member.StripeCustomerId, newCard);

            return card.Id;
        }
    }
}