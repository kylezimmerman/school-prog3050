/* Event.cs
 * Purpose: A class for store or online events which member's can register for
 * 
 * Revision History:
 *      Drew Matheson, 2015.10.02: Created
*/

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Veil.DataModels.Validation;

namespace Veil.DataModels.Models
{
    /// <summary>
    ///     An event which member's can register for
    /// </summary>
    public class Event
    {
        /// <summary>
        ///     The Id for the event
        /// </summary>
        [Key]
        public Guid Id { get; set; }

        /// <summary>
        ///     The event's name
        /// </summary>
        [Required]
        [StringLength(maximumLength: 255, ErrorMessageResourceName = nameof(ErrorMessages.StringLength), ErrorMessageResourceType = typeof(ErrorMessages))]
        public string Name { get; set; }

        /// <summary>
        ///     A description of the event
        /// </summary>
        [StringLength(maximumLength: 2048, ErrorMessageResourceName = nameof(ErrorMessages.StringLength), ErrorMessageResourceType = typeof(ErrorMessages))]
        [DataType(DataType.MultilineText)]
        public string Description { get; set; }

        /// <summary>
        ///     The date and time of the event
        /// </summary>
        [DataType(DataType.Text)]
        [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:yyyy-MM-dd}")]
        public DateTime Date { get; set; }

        /// <summary>
        ///     A string for how long the event is expected to be.
        /// </summary>
        /// <example>
        ///     <b>Examples:</b>
        ///     2 Hours
        ///     Unknown
        /// </example>
        [StringLength(maximumLength: 128, ErrorMessageResourceName = nameof(ErrorMessages.StringLength), ErrorMessageResourceType = typeof(ErrorMessages))]
        public string Duration { get; set; }

        /// <summary>
        ///     Collection navigation property for the member's registered for this event
        /// </summary>
        public virtual ICollection<Member> RegisteredMembers { get; set; }
    }
}