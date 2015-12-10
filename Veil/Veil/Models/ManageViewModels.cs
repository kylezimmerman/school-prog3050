/* ManageViewModels.cs
 * Purpose: View models used by ManageController
 * 
 * Revision History:
 *      Drew Matheson, 2015.09.25: Created
 */

using System.ComponentModel.DataAnnotations;
using System.Web.ModelBinding;
using Veil.DataModels.Models;
using Veil.DataModels.Validation;

namespace Veil.Models
{
    /// <summary>
    ///     View model for the manage index page. Also known as account settings
    /// </summary>
    public class IndexViewModel
    {
        /// <summary>
        ///     The user's phone number
        /// </summary>
        [DataType(DataType.PhoneNumber)]
        [RegularExpression(ValidationRegex.INPUT_PHONE,
            ErrorMessage = "Must be in the format 800-555-0199 or 800-555-0199, ext. 1234")]
        [StringLength(maximumLength: 24, ErrorMessageResourceName = nameof(ErrorMessages.StringLength),
            ErrorMessageResourceType = typeof (ErrorMessages))]
        [Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; }

        /// <summary>
        ///     The user's setting for whether to receive promotional emails
        /// </summary>
        [Display(Name = "Receive Promotional Emails?")]
        public bool ReceivePromotionalEmails { get; set; }

        /// <summary>
        ///     The user's first name
        /// </summary>
        [Required]
        [StringLength(maximumLength: 64, ErrorMessageResourceName = nameof(ErrorMessages.StringLength),
            ErrorMessageResourceType = typeof (ErrorMessages))]
        [Display(Name = "First Name")]
        public string MemberFirstName { get; set; }

        /// <summary>
        ///     The user's last name
        /// </summary>
        [Required]
        [StringLength(maximumLength: 64, ErrorMessageResourceName = nameof(ErrorMessages.StringLength),
            ErrorMessageResourceType = typeof (ErrorMessages))]
        [Display(Name = "Last Name")]
        public string MemberLastName { get; set; }

        /// <summary>
        ///     The user's email address
        /// </summary>
        [Required]
        [EmailAddress]
        [Display(Name = "Email Address")]
        public string MemberEmail { get; set; }

        /// <summary>
        ///     The user's setting for wishlist visibility
        /// </summary>
        [Display(Name = "Wishlist Visibility")]
        public WishListVisibility MemberVisibility { get; set; }

        /// <summary>
        ///     The count of the user's favorite platforms
        /// </summary>
        [BindNever]
        public int FavoritePlatformCount { get; set; }

        /// <summary>
        ///     The count of the user's favorite tags
        /// </summary>
        [BindNever]
        public int FavoriteTagCount { get; set; }
    }

    /// <summary>
    ///     View model for the change password page
    /// </summary>
    public class ChangePasswordViewModel
    {
        /// <summary>
        ///     The user's old password
        /// </summary>
        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Current Password")]
        public string OldPassword { get; set; }

        /// <summary>
        ///     The new password
        /// </summary>
        [Required]
        [StringLength(maximumLength: 100, MinimumLength = 6,
            ErrorMessageResourceName = nameof(ErrorMessages.StringLengthBetween),
            ErrorMessageResourceType = typeof (ErrorMessages))]
        [DataType(DataType.Password)]
        [Display(Name = "New Password")]
        public string NewPassword { get; set; }

        /// <summary>
        ///     Confirmation of the new password
        /// </summary>
        [DataType(DataType.Password)]
        [Display(Name = "Confirm New Password")]
        [Compare(nameof(NewPassword),
            ErrorMessage = "The new password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; }
    }
}