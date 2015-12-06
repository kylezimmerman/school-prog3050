/* EventListViewModel.cs
 * Purpose: View model for the Events list
 * 
 * Revision History:
 *      Isaac West, 2015.11.08: Created
 */ 

using System.Collections.Generic;
using Veil.DataModels.Models;

namespace Veil.Models
{
    /// <summary>
    ///     View model for the Events list
    /// </summary>
    public class EventListViewModel
    {
        /// <summary>
        ///     The list of events
        /// </summary>
        public IEnumerable<Event> Events { get; set; }

        /// <summary>
        ///     Flag indicating if only registered events should be shown
        /// </summary>
        public bool OnlyRegisteredEvents { get; set; }
    }

    /// <summary>
    ///     View model for an item in the Events list
    /// </summary>
    public class EventListItemViewModel
    {
        /// <summary>
        ///     The event
        /// </summary>
        public Event Event { get; set; }

        /// <summary>
        ///     Flag indicating if the event should only be shown if the current member is registered
        /// </summary>
        public bool OnlyRegisteredEvents { get; set; }

        /// <summary>
        ///     Flag indicating if the current member is registered for the event
        /// </summary>
        public bool CurrentMemberIsRegistered { get; set; }
    }
}