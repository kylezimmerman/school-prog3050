/* MemberAddress.cs
 * Purpose: A class to associate an address with a member
 * 
 * Revision History:
 *      Drew Matheson, 2015.10.03: Created
 */ 

using System;
using System.ComponentModel.DataAnnotations;

namespace Veil.Models
{
    /// <summary>
    /// A member's address (Billing or shipping)
    /// </summary>
    public class MemberAddress : Address
    {
        /// <summary>
        /// The Id for this address entry
        /// <remarks>
        ///     We override this so it is clearer that this class uses a composite primary key
        /// </remarks>
        /// </summary>
        [Key]
        public override Guid Id { get; set; }

        /// <summary>
        /// The Id for the member whose address this is
        /// </summary>
        [Key]
        public Guid MemberId { get; set; }

        /// <summary>
        /// Navigation property for the member whose address this is
        /// </summary>
        public virtual Member Member { get; set; }
    }
}