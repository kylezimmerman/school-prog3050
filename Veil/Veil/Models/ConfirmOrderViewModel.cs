/* ConfirmOrderViewModel.cs
 * Purpose: View models for the confirm order step of checkout
 * 
 * Revision History:
 *      Drew Matheson, 2015.11.14: Created
 */ 

using System;
using System.Collections.Generic;
using Veil.DataModels.Models;

namespace Veil.Models
{
    /// <summary>
    ///     View model for the order being confirmed
    /// </summary>
    public class ConfirmOrderViewModel
    {
        /// <summary>
        ///     The fullname the order is being placed un
        /// </summary>
        public string FullName { get; set; }

        /// <summary>
        ///     The address the order will be shipped to
        /// </summary>
        public Address Address { get; set; }

        /// <summary>
        ///     The name of the province the order will be shipped to
        /// </summary>
        public string ProvinceName { get; set; }

        /// <summary>
        ///     The name of the country the order will be shipped to
        /// </summary>
        public string CountryName { get; set; }

        /// <summary>
        ///     The formatted last 4 digits of the credit card being used for the order
        /// </summary>
        public string CreditCardLast4Digits { get; set; }

        /// <summary>
        ///     The cart items in the order being confirmed
        /// </summary>
        public virtual ICollection<ConfirmOrderCartItemViewModel> Items { get; set; }

        /// <summary>
        ///     The subtotal for the items in the order being confirmed
        /// </summary>
        public decimal ItemSubTotal { get; set; }

        /// <summary>
        ///     The tax amount for the order being confirmed
        /// </summary>
        public decimal TaxAmount { get; set; }

        /// <summary>
        ///     The shipping cost for the order being confirmed
        /// </summary>
        public decimal ShippingCost { get; set; }

        /// <summary>
        ///     The phone number associated with the order
        /// </summary>
        public string PhoneNumber { get; set; }
    }

    /// <summary>
    ///     View model for a cart item in the order being confirmed
    /// </summary>
    public class ConfirmOrderCartItemViewModel
    {
        /// <summary>
        ///     The Id of the product this cart item is for
        /// </summary>
        public Guid ProductId { get; set; }

        /// <summary>
        ///     The name of the cart items product
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        ///     The game platform name for the cart item
        /// </summary>
        public string PlatformName { get; set; }

        /// <summary>
        ///     Flag indicating if the cart item is for a new version
        /// </summary>
        public bool IsNew { get; set; }

        /// <summary>
        ///     The quantity of the cart item
        /// </summary>
        public int Quantity { get; set; }

        /// <summary>
        ///     The price of the cart item
        /// </summary>
        public decimal ItemPrice { get; set; }

        /// <summary>
        ///     The item total for the cart item
        /// </summary>
        public decimal ItemTotal => Quantity * ItemPrice;
    }
}