/* FriendsListViewModel.cs
 * Purpose: View model for the friends list
 * 
 * Revision History:
 *      Justin Coschi, 2015.11.10: Created
 */ 

using System.Collections.Generic;
using Veil.DataModels.Models;

namespace Veil.Models
{
    /// <summary>
    ///     View model for the <see cref="Friendship"/> list
    /// </summary>
    public class FriendsListViewModel
    {
        /// <summary>
        ///     The pending friend requests which the member sent
        /// </summary>
        public List<Member> PendingSentFriendships { get; set; }

        /// <summary>
        ///     The pending friend requests which the member received
        /// </summary>
        public List<Member> PendingReceivedFriendships { get; set; }

        /// <summary>
        ///     The confirmed friends
        /// </summary>
        public List<Member> ConfirmedFriends { get; set; }
    }
}