/* IPrincipalExtensions.cs
 * Purpose: Extension methods for IPrincipal
 * 
 * Revision History:
 *      Drew Matheson, 2015.11.18: Created
 */ 

using System.Security.Principal;
using Veil.DataModels;

namespace Veil.Extensions
{
    /// <summary>
    ///     Extension methods for <see cref="IPrincipal"/>
    /// </summary>
    public static class IPrincipalExtensions
    {
        /// <summary>
        ///     Checks if the <see cref="IPrincipal"/> is in the employee or admin roles
        /// </summary>
        /// <param name="principal">
        ///     The <see cref="IPrincipal"/> to check
        /// </param>
        /// <returns>
        ///     True if they are in either the Employee or the Admin role.
        ///     False otherwise.
        /// </returns>
        public static bool IsEmployeeOrAdmin(this IPrincipal principal)
        {
            return principal.IsInRole(VeilRoles.EMPLOYEE_ROLE) ||
                principal.IsInRole(VeilRoles.ADMIN_ROLE);
        }
    }
}