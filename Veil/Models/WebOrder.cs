/* WebOrder.cs
 * Purpose: A class for web orders and an enum for the statuses of an order
 * 
 * Revision History:
 *      Drew Matheson, 2015.10.03: Created
 */ 

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Veil.Models
{
    /// <summary>
    /// Enumeration of the status of an order
    /// </summary>
    public enum OrderStatus
    {
        /// <summary>
        /// The order isn't being handled by anyone
        /// </summary>
        PendingProcessing,

        /// <summary>
        /// Someone is currently processing the order
        /// </summary>
        BeingProcessed,

        /// <summary>
        /// The order has been processed
        /// </summary>
        Processed
    }

    // TODO: Add FKs for CreditCardPaymentInformation, and ShippingAddress
    /// <summary>
    /// A web order for a Member
    /// </summary>
    public class WebOrder
    {
        /// <summary>
        /// The Id for the order
        /// </summary>
        [Key]
        public Guid Id { get; set; } // TODO: Do we want to stick with GUID or should be use int?

        /// <summary>
        /// The Id of the member who made the order
        /// </summary>
        [Required]
        public string MemberId { get; set; }

        /// <summary>
        /// Navigation property for the Member who made the order
        /// </summary>
        public virtual Member Member { get; set; }

        /// <summary>
        /// Navigation property for the payment information for the order
        /// </summary>
        public virtual CreditCardPaymentInformation CreditCardPaymentInformation { get; set; }

        /// <summary>
        /// Navigation property for the shipping address for the order
        /// </summary>
        public virtual MemberAddress ShippingAddress { get; set; }

        /// <summary>
        /// The date and time the order was created
        /// </summary>
        public DateTime OrderDate { get; set; }

        /// <summary>
        /// Collection navigation property for the CartItems in the order
        /// </summary>
        public virtual ICollection<CartItem> OrderItems { get; set; }

        /// <summary>
        /// OrderStatus indicating the current status of the order
        /// </summary>
        public OrderStatus HasBeenProcessed { get; set; }

        /// <summary>
        /// The date and time of when the order was processed and shipped out
        /// </summary>
        public DateTime ProcessedDate { get; set; }
    }
}