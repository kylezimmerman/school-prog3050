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

        /// <summary>
        ///     A static class containing strings for use with the Roles property of <see cref="System.Web.Mvc.AuthorizeAttribute"/>
        /// </summary>
        public static class Authorize
        {
            /// <summary>
            ///     String for authorizing the Admin and Employee roles for an Action
            /// </summary>
            public const string Admin_Employee = ADMIN_ROLE + ", " + EMPLOYEE_ROLE;
        }
    }
}