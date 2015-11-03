/* VeilRoles.cs
 * Purpose: Static class to be used anywhere we use user roles
 * 
 * Revision History:
 *      Drew Matheson, 2015.10.28: Created
 */ 

namespace Veil.DataModels
{
    /// <summary>
    ///     A static class containing the names for user roles for Veil
    /// </summary>
    public static class VeilRoles
    {
        /// <summary>
        ///     The name for the admin role
        /// </summary>
        public const string ADMIN_ROLE = "Administrator";

        /// <summary>
        ///     The name for the employee role
        /// </summary>
        public const string EMPLOYEE_ROLE = "Employee";

        /// <summary>
        ///     The name for the member role
        /// </summary>
        public const string MEMBER_ROLE = "Member";
    }
}