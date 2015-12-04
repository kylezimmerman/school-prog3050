using System.ComponentModel.DataAnnotations;
using Veil.DataModels.Models;
using Veil.DataModels.Validation;

namespace Veil.Models
{
    public class IndexViewModel
    {
        [DataType(DataType.PhoneNumber)]
        [RegularExpression(ValidationRegex.INPUT_PHONE, ErrorMessage = "Must be in the format 800-555-0199 or 800-555-0199, ext. 1234")]
        [StringLength(maximumLength:24, ErrorMessageResourceName = nameof(ErrorMessages.StringLength), ErrorMessageResourceType = typeof(ErrorMessages))]
        [Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; }

        [Display(Name = "Receive Promotional Emails?")]
        public bool ReceivePromotionalEmail { get; set; }
        [Required]
        [StringLength(maximumLength: 64, ErrorMessageResourceName = nameof(ErrorMessages.StringLength), ErrorMessageResourceType = typeof(ErrorMessages))]
        public string MemberFirstName { get; set; }
        [Required]
        [StringLength(maximumLength: 64, ErrorMessageResourceName = nameof(ErrorMessages.StringLength), ErrorMessageResourceType = typeof(ErrorMessages))]
        public string MemberLastName { get; set; }

        [Required]
        [EmailAddress]
        public string MemberEmail { get; set; }
        public WishListVisibility MemberVisibility { get; set; }
        public int FavoritePlatformCount { get; set; }
        public int FavoriteTagCount { get; set; }
        public bool ReceivePromotionalEmals { get; set; }
    }

    public class ChangePasswordViewModel
    {
        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Current Password")]
        public string OldPassword { get; set; }

        [Required]
        [StringLength(maximumLength: 100, MinimumLength = 6, ErrorMessageResourceName = nameof(ErrorMessages.StringLengthBetween), ErrorMessageResourceType = typeof(ErrorMessages))]
        [DataType(DataType.Password)]
        [Display(Name = "New Password")]
        public string NewPassword { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirm New Password")]
        [Compare(nameof(NewPassword), ErrorMessage = "The new password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; }
    }
}