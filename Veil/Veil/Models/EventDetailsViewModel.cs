/* EventDetailsViewModel.cs
 * Purpose: View model for the Event details page
 * 
 * Revision History:
 *      Isaac West, 2015.12.05: Created
 */ 

using Veil.DataModels.Models;

namespace Veil.Models
{
    /// <summary>
    ///     View model used for the Event details page
    /// </summary>
    public class EventDetailsViewModel
    {
        /// <summary>
        ///     The event
        /// </summary>
        public Event Event { get; set; }

        /// <summary>
        ///     Flag indicating if the current member is registered for the <see cref="Event"/>
        /// </summary>
        public bool CurrentMemberIsRegistered { get; set; }
    }
}