using System.Collections.Generic;
using Veil.DataModels.Models;

namespace Veil.Models
{
    public class FriendsListViewModel
    {
        public List<Member> PendingSentFriendships { get; set; }
        public List<Member> PendingReceivedFriendships { get; set; }
        public List<Member> ConfirmedFriends { get; set; }
    }
}