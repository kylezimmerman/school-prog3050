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
        public static string AdminRole => "Administrator";

        /// <summary>
        ///     The name for the employee role
        /// </summary>
        public static string EmployeeRole => "Employee";

        /// <summary>
        ///     The name for the member role
        /// </summary>
        public static string MemberRole => "Member";
    }
}