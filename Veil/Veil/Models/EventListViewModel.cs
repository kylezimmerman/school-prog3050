using System.Collections.Generic;
using Veil.DataModels.Models;

namespace Veil.Models
{
    public class EventListViewModel
    {
        public IEnumerable<Event> Events { get; set; }
        public bool OnlyRegisteredEvents { get; set; }
    }
}