/* Friendship.cs
 * Purpose: Class for member friendships and an enum for friendship request statuses
 * 
 * Revision History:
 *      Drew Matheson, 2015.10.02: Created
 */

using System.ComponentModel.DataAnnotations;

namespace Veil.DataModels.Models
{
    /// <summary>
    /// Enumeration of the statuses for a friend request
    /// </summary>
    public enum FriendshipRequestStatus
    {
        /// <summary>
        /// The friend request is pending
        /// </summary>
        Pending,

        /// <summary>
        /// The friend request has been accepted
        /// </summary>
        Accepted
    }

    /// <summary>
    /// Represents a friendship between to members
    /// </summary>
    public class Friendship
    {
        /// <summary>
        /// The member Id of the person making the friend request
        /// </summary>
        [Key]
        public string RequesterId { get; set; }

        /// <summary>
        /// Navigation property for the Member making the friend request
        /// </summary>
        public virtual Member Requester { get; set; }

        /// <summary>
        /// The member Id of the person receiving the friend request
        /// </summary>
        [Key]
        public string ReceiverId { get; set; }

        /// <summary>
        /// Navigation property for the Member receiving the friend request
        /// </summary>
        public virtual Member Receiver { get; set; }

        /// <summary>
        /// The status of the friend request. If the request is denied, the request is deleted.
        /// </summary>
        [Required]
        public FriendshipRequestStatus RequestStatus { get; set; }
    }
}