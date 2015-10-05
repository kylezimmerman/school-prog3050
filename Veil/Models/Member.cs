/* Member.cs
 * Purpose: Class for site members
 * 
 * Revision History:
 *      Drew Matheson, 2015.10.02: Created
 */ 

using System.Collections.Generic;

namespace Veil.Models
{
    /// <summary>
    /// A member of the website
    /// </summary>
    public class Member : Person
    {
        /// <summary>
        /// Navigation property for the Member's cart
        /// </summary>
        public virtual Cart Cart { get; set; }

        /// <summary>
        /// Collection navigation property for the Member's favorite platforms
        /// </summary>
        public virtual ICollection<Platform> FavoritePlatforms { get; set; }

        /// <summary>
        /// Collection navigation property for the Member's favorite tags/categories
        /// </summary>
        public virtual ICollection<Tag> FavoriteTags { get; set; }

        /// <summary>
        /// Collection navigation property for the Member's wishlist items
        /// </summary>
        public virtual ICollection<Product> Wishlist { get; set; }

        /// <summary>
        /// Collection navigation property for the Event's the Member is registered for
        /// </summary>
        public virtual ICollection<Event> RegisteredEvents { get; set; }

        /// <summary>
        /// Collection navigation property for the Member's saved shipping addresses
        /// </summary>
        public virtual ICollection<MemberAddress> ShippingAddresses { get; set; }

        /// <summary>
        /// Collection navigation property for the Member's saved billing addresses
        /// </summary>
        public virtual ICollection<MemberAddress> BillingAddresses{ get; set; }

        /// <summary>
        /// Collection navigation property for the Member's saved payment information
        /// </summary>
        public virtual ICollection<CreditCardPaymentInformation> PaymentInformation { get; set; }
    }
}