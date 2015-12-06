/* AccountViewModels.cs
 * Purpose: View models used by AccountController
 * 
 * Revision History:
 *      Drew Matheson, 2015.10.27: Created
 */ 

using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Veil.DataModels.Models;

namespace Veil.Models
{
    /// <summary>
    ///     View Model used for the Login/Register combination page
    /// </summary>
    public class LoginRegisterViewModel
    {
        /// <summary>
        ///     The view model for the login portion of the page
        /// </summary>
        public LoginViewModel LoginViewModel { get; set; }

        /// <summary>
        ///     The view model for the register portion of the page
        /// </summary>
        public RegisterViewModel RegisterViewModel { get; set; }
    }

    /// <summary>
    ///     View model for logging in
    /// </summary>
    public class LoginViewModel
    {
        /// <summary>
        ///     The email account the user is trying to log in with
        /// </summary>
        [Required]
        [Display(Name = "Email")]
        [EmailAddress]
        public string LoginEmail { get; set; }

        /// <summary>
        ///     The password for the account the user is trying to log in with
        /// </summary>
        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string LoginPassword { get; set; }

        /// <summary>
        ///     Flag indicating if the user wants the login to be persisted
        /// </summary>
        [Display(Name = "Remember me?")]
        public bool RememberMe { get; set; }
    }

    /// <summary>
    ///     View model for registration
    /// </summary>
    public class RegisterViewModel
    {
        /// <summary>
        ///     Required empty constructor which also sets default values
        /// </summary>
        public RegisterViewModel()
        {
            ReceivePromotionalEmail = true;
            WishListVisibility = WishListVisibility.FriendsOnly;
        }

        /// <summary>
        ///     The email address for the new account
        ///     This must be unique across the site.
        /// </summary>
        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; }

        /// <summary>
        ///     The username for the new account
        ///     This must be unique across the site.
        /// </summary>
        [Required]
        [MaxLength(255)]
        [Display(Name = "Username")]
        public string Username { get; set; }

        /// <summary>
        ///     The first name of the person creating the new account
        /// </summary>
        [Required]
        [Display(Name = "First Name")]
        [StringLength(maximumLength: 64, MinimumLength = 1)]
        public string FirstName { get; set; }

        /// <summary>
        ///     The last name of the person creating the new account
        /// </summary>
        [Required]
        [Display(Name = "Last Name")]
        [StringLength(maximumLength: 64, MinimumLength = 1)]
        public string LastName { get; set; }

        /// <summary>
        ///     The password for the new account
        /// </summary>
        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.",
            MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; }

        /// <summary>
        ///     Confirmation of the password
        /// </summary>
        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        [Compare("Password",
            ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; }

        /// <summary>
        ///     Flag indicating if the user wishes to receive promotional email
        /// </summary>
        [Display(Name = "Receive Promotional Emails?")]
        public bool ReceivePromotionalEmail { get; set; }

        /// <summary>
        ///     The initial wishlist visibility status for the new account
        /// </summary>
        [Display(Name = "Wish List Visibility")]
        [DefaultValue(WishListVisibility.FriendsOnly)]
        public WishListVisibility WishListVisibility { get; set; }
    }

    /// <summary>
    ///     View model for the reset password page which is used when a password is forgotten
    /// </summary>
    public class ResetPasswordViewModel
    {
        /// <summary>
        ///     The email address associated with the account the password was forgotten for.
        ///     <br/>
        ///     This is a way of ensuring that even if someone gets the reset link they 
        ///     still need to know the email address.
        /// </summary>
        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; }

        /// <summary>
        ///     The new password
        /// </summary>
        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.",
            MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; }

        /// <summary>
        ///     Confirmation of the new password
        /// </summary>
        [DataType(DataType.Password)]
        [Display(Name = "Confirm Password")]
        [Compare("Password",
            ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; }

        /// <summary>
        ///     The password reset code which was generated to reset the forgotten password
        /// </summary>
        public string Code { get; set; }
    }

    /// <summary>
    ///     View model for the forgot password page
    /// </summary>
    public class ForgotPasswordViewModel
    {
        /// <summary>
        ///     The email address associated with the account the password was forgotten for
        /// </summary>
        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; }
    }
}