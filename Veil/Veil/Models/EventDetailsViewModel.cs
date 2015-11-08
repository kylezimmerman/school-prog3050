using Veil.DataModels.Models;

namespace Veil.Models
{
    public class EventDetailsViewModel
    {
        public Event Event { get; set; }
        public bool CurrentMemberIsRegistered { get; set; }
    }
}