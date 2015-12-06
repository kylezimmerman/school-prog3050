/* CompanyViewModel.cs
 * Purpose: View model for the manage companies page
 * 
 * Revision History:
 *      Isaac West, 2015.12.02: Created
 */ 

using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;
using Veil.DataModels.Validation;

namespace Veil.Models
{
    /// <summary>
    ///     View model for the manage companies page
    /// </summary>
    public class CompanyViewModel
    {
        /// <summary>
        ///     The name for the new company
        /// </summary>
        [Required]
        [StringLength(512, ErrorMessageResourceName = nameof(ErrorMessages.StringLength),
            ErrorMessageResourceType = typeof (ErrorMessages))]
        [DisplayName("New Company Name")]
        public string NewCompany { get; set; }

        /// <summary>
        ///     A select list of safely deletable companies
        /// </summary>
        public SelectList Deletable { get; set; }
    }
}