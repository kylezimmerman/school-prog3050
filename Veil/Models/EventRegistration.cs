using System.ComponentModel.DataAnnotations;

namespace Veil.Models
{
    public class EventRegistration
    {
        [Key]
        public string EventId { get; set; }

        public Event Event { get; set; }

        [Key]
        public string MemberId { get; set; }

        public Member Member { get; set; }
    }
}