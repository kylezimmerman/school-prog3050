using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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
        ///     We override this so it is more clear that this class uses a composite primary key
        /// </remarks>
        /// </summary>
        [Key, Column(Order = 0)]
        public override Guid Id { get; set; }

        /// <summary>
        /// The Id for the member whose address this is
        /// </summary>
        [Key, Column(Order = 1)]
        [ForeignKey(nameof(Member))]
        public Guid MemberId { get; set; }

        /// <summary>
        /// Navigation property for the member whose address this is
        /// </summary>
        public virtual Member Member { get; set; }
    }
}