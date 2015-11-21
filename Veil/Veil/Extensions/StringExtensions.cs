/* StringExtensions.cs
 * Purpose: String extension methods
 * 
 * Revision History:
 *      Drew Matheson, 2015.11.18: Created
 */ 

namespace Veil.Extensions
{
    /// <summary>
    ///     String extension methods
    /// </summary>
    public static class StringExtensions
    {
        /// <summary>
        ///     Formats the last 4 digits of a card
        /// </summary>
        /// <param name="last4Digits">
        ///     The last 4 digits of a card    
        /// </param>
        /// <returns>
        ///     A string in the format **** **** **** 4444
        ///     where 4444 is the last 4 digits
        /// </returns>
        public static string FormatLast4Digits(this string last4Digits)
        {
            return last4Digits.PadLeft(16, '*').Insert(4, " ").Insert(9, " ").Insert(14, " ");
        }
    }
}