using System;
using Veil.DataModels.Models;

namespace Veil.Models
{
    public class WebOrderCheckoutDetails
    {
        /// <summary>
        ///     The Id of the shipping address the member used to make the order
        /// </summary>
        public Guid? MemberAddressId { get; set; }

        /// <summary>
        ///     The Id of the MemberCreditCard used for this order.
        /// </summary>
        public Guid MemberCreditCardId { get; set; }

        /// <summary>
        ///     Contains the address information for the web order
        /// </summary>
        public Address Address { get; set; }

        /// <summary>
        ///     The Province Code associated with the Address property
        /// </summary>
        public string ProvinceCode { get; set; }

        /// <summary>
        ///     The CountryCode associated with the Address property
        /// </summary>
        public string CountryCode { get; set; }

    }
}