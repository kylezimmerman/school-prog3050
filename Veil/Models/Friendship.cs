using System.ComponentModel.DataAnnotations;

namespace Veil.Models
{
    // Figure out how to make it unique (requester, requestee) (requestee, requester)
    public class Friendship
    {
        [Key]
        public string RequesterId { get; set; }

        public virtual Member Requester { get; set; }

        [Key]
        public string RequesteeId { get; set; }

        public virtual Member Requestee { get; set; }

        [Required]
        public FriendshipRequestStatus RequestStatus { get; set; }
    }
}