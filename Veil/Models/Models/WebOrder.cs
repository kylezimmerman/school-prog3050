/* WebOrder.cs
 * Purpose: A class for web orders and an enum for the statuses of an order
 * 
 * Revision History:
 *      Drew Matheson, 2015.10.03: Created
 */

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Veil.DataModels.Models
{
    /// <summary>
    ///     Enumeration of the status of an order
    /// </summary>
    public enum OrderStatus
    {
        /// <summary>
        ///     The order isn't being handled by anyone
        /// </summary>
        [Display(Name = "Pending Processing")]
        PendingProcessing,

        /// <summary>
        ///     Someone is currently processing the order
        /// </summary>
        [Display(Name = "Being Processed")]
        BeingProcessed,

        /// <summary>
        ///     The order has been processed
        /// </summary>
        Processed,

        /// <summary>
        ///     The user cancelled the order.
        /// </summary>
        [Display(Name = "Cancelled by User")]
        UserCancelled,

        /// <summary>
        ///     An employee cancelled the order
        /// </summary>
        [Display(Name = "Cancelled by Employee")]
        EmployeeCancelled
    }

    /// <summary>
    ///     A web order for a Member
    /// </summary>
    public class WebOrder
    {
        /// <summary>
        ///     The Id for the order
        /// </summary>
        [Key]
        public long Id { get; set; }

        /// <summary>
        ///     The Id of the member who made the order
        /// </summary>
        [Required]
        public Guid MemberId { get; set; }

        /// <summary>
        ///     Navigation property for the Member who made the order
        /// </summary>
        public virtual Member Member { get; set; }

        /// <summary>
        ///     The last 4 digits of the credit card used for this order
        /// </summary>
        [Required]
        [StringLength(maximumLength: 4, MinimumLength = 4)]
        public string CreditCardLast4Digits { get; set; }

        /// <summary>
        ///     Contains the address information for the web order
        /// </summary>
        public Address Address { get; set; }

        /// <summary>
        /// The province code for this Address's Province
        /// </summary>
        [Required]
        [StringLength(2, MinimumLength = 2)]
        public string ProvinceCode { get; set; }

        /// <summary>
        ///     Navigation property for this Address's Province
        /// </summary>
        public virtual Province Province { get; set; }

        /// <summary>
        /// The country code for this Address's Country
        /// </summary>
        [StringLength(2, MinimumLength = 2)]
        [Required]
        public string CountryCode { get; set; }

        /// <summary>
        ///     Navigation property for this Address's Country
        /// </summary>
        public virtual Country Country { get; set; } 

        /// <summary>
        ///     The Id for the Charge for this WebOrder as returned from Stripe
        /// </summary>
        [Required]
        [MaxLength(255)]
        public string StripeChargeId { get; set; }

        /// <summary>
        ///     The date and time the order was created
        /// </summary>
        public DateTime OrderDate { get; set; }

        /// <summary>
        ///     Collection navigation property for the CartItems in the order
        /// </summary>
        public virtual ICollection<OrderItem> OrderItems { get; set; }

        /// <summary>
        ///     OrderStatus indicating the current status of the order
        /// </summary>
        public OrderStatus OrderStatus { get; set; }

        /// <summary>
        ///     The date and time of when the order was processed and shipped out
        /// </summary>
        public DateTime? ProcessedDate { get; set; }

        /// <summary>
        ///     A message explaining why the order was cancelled by an employee
        /// </summary>
        [MaxLength(512)]
        public string ReasonForCancellationMessage { get; set; }

        /// <summary>
        ///     The total tax amount for the order
        /// </summary>
        public decimal TaxAmount { get; set; }

        /// <summary>
        ///     The shipping cost for the order
        /// </summary>
        public decimal ShippingCost { get; set; }

        /// <summary>
        ///     The cart subtotal
        /// </summary>
        public decimal OrderSubtotal { get; set; }
    }
}