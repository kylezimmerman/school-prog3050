/* EventViewModel.cs
 * Purpose: View model for the Event model
 * 
 * Revision History:
 *      Justin Coschi, 2015.11.06: Created
 */ 

using System;
using System.ComponentModel.DataAnnotations;
using System.Web.ModelBinding;
using Veil.DataModels.Models;

namespace Veil.Models
{
    /// <summary>
    ///     View model for <see cref="Event"/> model
    /// </summary>
    public class EventViewModel : Event
    {
        /// <summary>
        ///     The start date of the event
        /// </summary>
        [DataType(DataType.Date)]
        public new DateTime Date { get; set; }

        /// <summary>
        ///     The start time of the event
        /// </summary>
        [DataType(DataType.Time)]
        public DateTime Time { get; set; }

        /// <summary>
        ///     The combined start date and time of the event
        /// </summary>
        [BindNever]
        public DateTime DateTime
            => new DateTime(Date.Year, Date.Month, Date.Day, Time.Hour, Time.Minute, Time.Second);
    }
}