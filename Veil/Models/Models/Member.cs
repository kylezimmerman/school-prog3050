/* Member.cs
 * Purpose: Class for site members and an enum for their wishlist visibility status
 * 
 * Revision History:
 *      Drew Matheson, 2015.10.02: Created
 */

using System.Collections.Generic;

namespace Veil.DataModels.Models
{
    /// <summary>
    /// Enumeration of the visibility statuses for a member's wishlist
    /// </summary>
    public enum WishListVisibility
    {
        /// <summary>
        /// Anyone can view the wishlist
        /// </summary>
        Public,

        /// <summary>
        /// Only the member and their friends can view the wishlist
        /// </summary>
        FriendsOnly,

        /// <summary>
        /// Only the member can view the wishlist
        /// </summary>
        Private
    }

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
        /// WishListVisibility indicating who can view the member's wishlist
        /// </summary>
        public WishListVisibility WishListVisibility { get; set; }

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
        /// Collection navigation property for the Member's saved payment information
        /// </summary>
        public virtual ICollection<MemberCreditCard> CreditCards { get; set; }
    }
}