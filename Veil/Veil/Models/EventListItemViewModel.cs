using Veil.DataModels.Models;

namespace Veil.Models
{
    public class EventListItemViewModel
    {
        public Event Event { get; set; }
        public bool OnlyRegisteredEvents { get; set; }
        public bool CurrentMemberIsRegistered { get; set; }
    }
}