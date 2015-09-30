using System.ComponentModel.DataAnnotations;

namespace Veil.Models
{
    public class Friendship
    {
        [Key]
        public string RequesterId { get; set; }

        public Member Requester { get; set; }

        [Key]
        public string RequesteeId { get; set; }

        public Member Requestee { get; set; }

        [Required]
        public FriendshipRequestStatus RequestStatus { get; set; }
    }
}