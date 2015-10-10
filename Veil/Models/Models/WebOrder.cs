/* WebOrder.cs
 * Purpose: A class for web orders and an enum for the statuses of an order
 * 
 * Revision History:
 *      Drew Matheson, 2015.10.03: Created
 */

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Veil.DataModels.Models
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
        Processed,

        /// <summary>
        /// The user cancelled the order.
        /// </summary>
        UserCancelled,

        /// <summary>
        /// An employee cancelled the order
        /// </summary>
        EmployeeCancelled
    }

    /// <summary>
    /// A web order for a Member
    /// </summary>
    public class WebOrder
    {
        /// <summary>
        /// The Id for the order
        /// </summary>
        [Key]
        public long Id { get; set; }

        /// <summary>
        /// The Id of the member who made the order
        /// </summary>
        [Required]
        public Guid MemberId { get; set; }

        /// <summary>
        /// Navigation property for the Member who made the order
        /// </summary>
        public virtual Member Member { get; set; }

        /// <summary>
        /// The credit card number used for this order.
        /// This is part of the composite key for MemberCreditCard
        /// </summary>
        public string CreditCardNumber { get; set; }

        /// <summary>
        /// Navigation property for the payment information for the order
        /// </summary>
        public virtual MemberCreditCard MemberCreditCard { get; set; }

        /// <summary>
        /// The Id of the shipping address the member used to make the order
        /// </summary>
        public Guid ShippingAddressId { get; set; }

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
        public virtual ICollection<OrderItem> OrderItems { get; set; }

        /// <summary>
        /// OrderStatus indicating the current status of the order
        /// </summary>
        public OrderStatus OrderStatus { get; set; }

        /// <summary>
        /// The date and time of when the order was processed and shipped out
        /// </summary>
        public DateTime ProcessedDate { get; set; }

        /// <summary>
        /// A message explaining why the order was cancelled by an employee
        /// </summary>
        [MaxLength(512)]
        public string ReasonForCancellationMessage { get; set; }
    }
}