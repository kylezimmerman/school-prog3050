/* WishlistPhysicalGameProductViewModel.cs
 * Purpose: View model for a PhysicalGameProduct on a wishlist
 * 
 * Revision History:
 *      Isaac West, 2015.11.07: Created
 */ 

namespace Veil.Models
{
    /// <summary>
    ///     View model for a PhysicalGameProduct on a wishlist
    /// </summary>
    public class WishlistPhysicalGameProductViewModel : PhysicalGameProductViewModel
    {
        /// <summary>
        ///     Flag indicating if the wishlist owner is the same as the current member
        /// </summary>
        public bool MemberIsCurrentUser { get; set; }
    }
}