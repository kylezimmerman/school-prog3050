using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web.ModelBinding;
using System.Web.Mvc;
using Veil.DataModels.Models;

namespace Veil.Models
{
    public class ManageCreditCardViewModel
    {
        // ReSharper disable once UnassignedGetOnlyAutoProperty
        /// <summary>
        ///     Property only used to generate the view
        /// </summary>
        public CreditCardViewModel CreditCard { get; }

        /// <summary>
        ///     A list of the countries supported
        /// </summary>
        [BindNever]
        public IList<Country> Countries { get; set; }

        /// <summary>
        ///     A list of the current Member's Credit Cards
        /// </summary>
        [BindNever]
        public IEnumerable<SelectListItem> CreditCards { get; set; } 

        /// <summary>
        ///     A list of the current year and the next 19
        /// </summary>
        [BindNever]
        [Required]
        public IEnumerable<SelectListItem> Years => new SelectList(Enumerable.Range(DateTime.Today.Year, 20)); 

        /// <summary>
        ///     The list of months
        /// </summary>
        [BindNever]
        [Required]
        public IEnumerable<SelectListItem> Months => new List<SelectListItem>
        {
            new SelectListItem {Text = "01 - January", Value = "01" },
            new SelectListItem {Text = "02 - February", Value = "02" },
            new SelectListItem {Text = "03 - March", Value = "03" },
            new SelectListItem {Text = "04 - April", Value = "04" },
            new SelectListItem {Text = "05 - May", Value = "05" },
            new SelectListItem {Text = "06 - June", Value = "06" },
            new SelectListItem {Text = "07 - July", Value = "07" },
            new SelectListItem {Text = "08 - August", Value = "08" },
            new SelectListItem {Text = "09 - September", Value = "09" },
            new SelectListItem {Text = "10 - October", Value = "10" },
            new SelectListItem {Text = "11 - November", Value = "11" },
            new SelectListItem {Text = "12 - December", Value = "12" }
        }; 
    }
}