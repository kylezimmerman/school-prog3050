using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Veil.DataModels.Models;

namespace Veil.Models
{
    /// <summary>
    ///     View Model used for the Login page
    /// </summary>
    public class LoginRegisterViewModel
    {
        public LoginViewModel LoginViewModel { get; set; }

        public RegisterViewModel RegisterViewModel { get; set; }
    }

    public class LoginViewModel
    {
        [Required]
        [Display(Name = "Email")]
        [EmailAddress]
        public string LoginEmail { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string LoginPassword { get; set; }

        [Display(Name = "Remember me?")]
        public bool RememberMe { get; set; }
    }

    public class RegisterViewModel
    {
        public RegisterViewModel()
        {
            ReceivePromotionalEmail = true;
            WishListVisibility = WishListVisibility.FriendsOnly;
        }

        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required]
        [MaxLength(255)]
        [Display(Name = "Username")]
        public string Username { get; set; }

        [Required]
        [Display(Name = "First Name")]
        [StringLength(maximumLength: 64, MinimumLength = 1)]
        public string FirstName { get; set; }

        [Required]
        [Display(Name = "Last Name")]
        [StringLength(maximumLength: 64, MinimumLength = 1)]
        public string LastName { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        [System.ComponentModel.DataAnnotations.Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; }

        [Display(Name = "Receive Promotional Emails?")]
        public bool ReceivePromotionalEmail { get; set; }

        [Display(Name = "Wish List Visibility")]
        [DefaultValue(WishListVisibility.FriendsOnly)]
        public WishListVisibility WishListVisibility { get; set; }
    }

    public class ResetPasswordViewModel
    {
        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirm Password")]
        [System.ComponentModel.DataAnnotations.Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; }

        public string Code { get; set; }
    }

    public class ForgotPasswordViewModel
    {
        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; }
    }
}
