/* ErrorViewModel.cs
 * Purpose: View model for an error
 * 
 * Revision History:
 *      Drew Matheson, 2015.11.06: Created
 */ 

namespace Veil.Models
{
    /// <summary>
    ///     View Model for Error views
    /// </summary>
    public class ErrorViewModel
    {
        /// <summary>
        ///     A user friendly title for the error
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        ///     A user friendly message for the error
        /// </summary>
        public string Message { get; set; }
    }
}