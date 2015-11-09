/* Event.cs
 * Purpose: A class for store or online events which member's can register for
 * 
 * Revision History:
 *      Drew Matheson, 2015.10.02: Created
*/

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

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
        [Key]
        public Guid Id { get; set; }

        /// <summary>
        /// The event's name
        /// </summary>
        [Required]
        [MaxLength(255)]
        public string Name { get; set; }

        /// <summary>
        /// A description of the event
        /// </summary>
        [MaxLength(2048)]
        public string Description { get; set; }

        /// <summary>
        /// The date and time of the event
        /// </summary>
        public DateTime Date { get; set; }

        /// <summary>
        /// A string for how long the event is expected to be.
        /// <example>
        ///     2 Hours
        ///     Unknown
        /// </example>
        /// </summary>
        [MaxLength(128)]
        public string Duration { get; set; }

        /// <summary>
        /// Collection navigation property for the member's registered for this event
        /// </summary>
        public virtual ICollection<Member> RegisteredMembers { get; set; }
    }
}