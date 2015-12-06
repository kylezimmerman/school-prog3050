/* AgeGateViewModel.cs
 * Purpose: View model for the AgeGate index page/action
 * 
 * Revision History:
 *      Drew Matheson, 2015.12.04: Created
 */ 

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web.ModelBinding;
using System.Web.Mvc;
using Veil.DataModels.Validation;

namespace Veil.Models
{
    public class AgeGateViewModel
    {
        /// <summary>
        ///     Instantiates a new instance of AgeGateViewModel with default values
        /// </summary>
        public AgeGateViewModel() { } // Required empty constructor

        /// <summary>
        ///     Instantiates a new instance of AgeGateViewModel with the provided arguments
        /// </summary>
        /// <param name="returnUrl">
        ///     The Url to return to if the data is valid
        /// </param>
        /// <param name="name">
        ///     The name to display as part of the page title
        /// </param>
        public AgeGateViewModel(string returnUrl, string name = "")
        {
            ReturnUrl = returnUrl;
            Name = name;
        }

        /// <summary>
        ///     A list of the current year and the past 120 years
        /// </summary>
        [BindNever]
        public IEnumerable<SelectListItem> Years
            => new SelectList(Enumerable.Range(DateTime.UtcNow.Year - 119, 120), DateTime.UtcNow.Year);

        /// <summary>
        ///     The list of months
        /// </summary>
        [BindNever]
        public IEnumerable<SelectListItem> Months => new List<SelectListItem>
        {
            new SelectListItem { Text = "01 - January", Value = "01" },
            new SelectListItem { Text = "02 - February", Value = "02" },
            new SelectListItem { Text = "03 - March", Value = "03" },
            new SelectListItem { Text = "04 - April", Value = "04" },
            new SelectListItem { Text = "05 - May", Value = "05" },
            new SelectListItem { Text = "06 - June", Value = "06" },
            new SelectListItem { Text = "07 - July", Value = "07" },
            new SelectListItem { Text = "08 - August", Value = "08" },
            new SelectListItem { Text = "09 - September", Value = "09" },
            new SelectListItem { Text = "10 - October", Value = "10" },
            new SelectListItem { Text = "11 - November", Value = "11" },
            new SelectListItem { Text = "12 - December", Value = "12" }
        };

        /// <summary>
        ///     The list of numeric days of the month
        /// </summary>
        [BindNever]
        public IEnumerable<SelectListItem> Days => new SelectList(Enumerable.Range(1, 31));

        /// <summary>
        ///     The Url to return to
        /// </summary>
        public string ReturnUrl { get; set; }

        /// <summary>
        ///     The name to display in the title
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        ///     The birthdate day
        /// </summary>
        [Required]
        [DisplayName("Day")]
        [Range(1, 31, ErrorMessageResourceName = nameof(ErrorMessages.Range),
            ErrorMessageResourceType = typeof (ErrorMessages))]
        public int Day { get; set; }

        /// <summary>
        ///     The birthdate month
        /// </summary>
        [Range(1, 12, ErrorMessageResourceName = nameof(ErrorMessages.Range),
            ErrorMessageResourceType = typeof (ErrorMessages))]
        [DisplayName("Month")]
        [Required]
        public int Month { get; set; }

        /// <summary>
        ///     The birthdate year
        /// </summary>
        [DisplayName("Year")]
        [Required]
        public int Year { get; set; } = DateTime.UtcNow.Year; // So it is the selected value
    }
}