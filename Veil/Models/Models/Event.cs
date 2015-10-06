/* Event.cs
 * Purpose: // TODO: Figure out what an event really entails
 * 
 * Revision History:
 *      Drew Matheson, 2015.10.02: Created
*/

using System;
using System.Collections.Generic;

namespace Veil.DataModels.Models
{
    /// <summary>
    /// An event which member's can register for
    /// </summary>
    public class Event
    {
        /// <summary>
        /// The Id for the event
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// The event's name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// A description of the event
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// The date and time of the event
        /// </summary>
        public DateTime Date { get; set; }

        /// <summary>
        /// Collection navigation property for the member's registered for this event
        /// </summary>
        public virtual ICollection<Member> RegisteredMembers { get; set; }
    }
}