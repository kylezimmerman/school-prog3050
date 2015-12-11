/* DateFilteredListViewModel.cs
 * Purpose: A view model for date filtered list of a generic object
 * 
 * Revision History:
 *      Drew Matheson, 2015.11.24: Created
 */ 

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Veil.Models.Reports
{
    /// <summary>
    ///     View model containing the filtered dates
    /// </summary>
    public class DateFilteredViewModel
    {
        /// <summary>
        ///     The starting date used when filtering.
        ///     If this is null, no date filtering has occured.
        /// </summary>
        [DataType(DataType.Text)]
        [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:yyyy-MM-dd}")]
        public DateTime? StartDate { get; set; }

        /// <summary>
        ///     The ending date used when filtering.
        /// </summary>
        [DataType(DataType.Text)]
        [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:yyyy-MM-dd}")]
        public DateTime? EndDate { get; set; }
    }

    /// <summary>
    ///     Generic view model for a date filtered list
    /// </summary>
    /// <typeparam name="T">The type of the list items</typeparam>
    public class DateFilteredListViewModel<T> : DateFilteredViewModel where T : class
    {
        /// <summary>
        ///     The list of items filtered within the optional date range
        /// </summary>
        public List<T> Items { get; set; }
    }
}