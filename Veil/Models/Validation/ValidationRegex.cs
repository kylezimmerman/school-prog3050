/* ValidationRegex.cs
 * Purpose: A class to hold all the input validation regex patterns used within Veil
 * 
 * Revision History:
 *      Drew Matheson, 2015.11.04: Created
 */ 

namespace Veil.DataModels.Validation
{
    /// <summary>
    ///     Regex pattern constants for input validation within Veil
    /// </summary>
    public static class ValidationRegex
    {
        /// <summary>
        ///     Pattern for input phone numbers
        /// </summary>
        /// <example>
        ///     (800)555-0199
        ///     800-555-0199
        ///     (800)555-0199, ext. 5555
        ///     800-555-0199, ext. 5555
        /// </example>
        public const string INPUT_PHONE = @"^(\(?\d{3}(\)|-)\d{3}-\d{4}(, (EXT|ext). \d{1,4})?)$";

        /// <summary>
        ///     Pattern for persisted phone numbers
        /// </summary>
        /// <example>
        ///     800-555-0199
        ///     800-555-0199, ext. 5555    
        /// </example>
        public const string STORED_PHONE = @"^(\d{3}-\d{3}-\d{4}(, ext. \d{1,4})?)$";

        /// <summary>
        ///     Pattern for Used SKUs of Physical Game Products
        /// </summary>
        public const string PHYSICAL_GAME_PRODUCT_USED_SKU = @"^1\d{12}$";

        /// <summary>
        ///     Pattern for New SKUs of Physical Game Products
        /// </summary>
        public const string PHYSICAL_GAME_PRODUCT_NEW_SKU = @"^0\d{12}$";
    }
}