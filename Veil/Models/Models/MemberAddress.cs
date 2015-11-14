/* MemberAddress.cs
 * Purpose: A class to associate an address with a member
 * 
 * Revision History:
 *      Drew Matheson, 2015.10.03: Created
 */

using System;
using System.ComponentModel.DataAnnotations;

namespace Veil.DataModels.Models
{
    /// <summary>
    ///     A member's address (Billing or shipping)
    /// </summary>
    public class MemberAddress
    {
        /// <summary>
        ///     The Id for this address entry
        /// </summary>
        [Key]
        public virtual Guid Id { get; set; }

        /// <summary>
        ///     The Id for the member whose address this is
        /// </summary>
        public Guid MemberId { get; set; }

        /// <summary>
        ///     Navigation property for the member whose address this is
        /// </summary>
        public virtual Member Member { get; set; }

        /// <summary>
        ///     Contains the address information for this MemberAddress
        /// </summary>
        [Required]
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
    }
}