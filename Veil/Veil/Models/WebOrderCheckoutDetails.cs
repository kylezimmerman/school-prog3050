using System;
using Veil.DataModels.Models;

namespace Veil.Models
{
    public class WebOrderCheckoutDetails
    {
        private Guid? memberAddressId;
        private Address address;
        private Guid? memberCreditCardId;
        private string stripeCardToken;

        /// <summary>
        ///     The Id of the shipping address the member used to make the order
        ///     Nulls Address, ProvinceCode, and CountryCode when set.
        /// </summary>
        /// <remarks>
        ///     Either this or Address should be set.
        /// </remarks>
        public Guid? MemberAddressId
        {
            get
            {
                return memberAddressId;
            }
            set
            {
                address = null;
                ProvinceCode = null;
                CountryCode = null;

                memberAddressId = value;
            }
        }

        /// <summary>
        ///     Contains the address information for the web order
        ///     Nulls MemberAddressId when set.
        /// </summary>
        /// <remarks>
        ///     Either this or MemberAddressId should be set.
        /// </remarks>
        public Address Address
        {
            get
            {
                return address;
            }
            set
            {
                memberAddressId = null;

                address = value;
            }
        }

        /// <summary>
        ///     The Province Code associated with the Address property
        /// </summary>
        public string ProvinceCode { get; set; }

        /// <summary>
        ///     The CountryCode associated with the Address property
        /// </summary>
        public string CountryCode { get; set; }

        /// <summary>
        ///     The Id of the MemberCreditCard used for this order.
        ///     Nulls StripeCardToken and Last4Digits when set.
        /// </summary>
        /// <remarks>
        ///     Either this or StripeCardToken should be set.
        /// </remarks>
        public Guid? MemberCreditCardId
        {
            get
            {
                return memberCreditCardId;
            }
            set
            {
                stripeCardToken = null;

                memberCreditCardId = value;
            }
        }

        /// <summary>
        ///     The onetime use Stripe Card Token for this order.
        ///     Nulls MemberCreditCardId when set.
        /// </summary>
        /// <remarks>
        ///     Either this or MemberCreditCardId should be set. 
        /// </remarks>
        public string StripeCardToken
        {
            get
            {
                return stripeCardToken;
            }
            set
            {
                memberCreditCardId = null;
                stripeCardToken = value;
            }
        }
    }
}