using System.Security.Principal;
using Veil.DataModels;

namespace Veil.Extensions
{
    public static class IPrincipleExtensions
    {
        public static bool IsEmployeeOrAdmin(this IPrincipal principal)
        {
            return principal.IsInRole(VeilRoles.EMPLOYEE_ROLE) ||
                principal.IsInRole(VeilRoles.ADMIN_ROLE)/* ||
                System.Web.HttpContext.Current.IsDebuggingEnabled*/; /* TODO: Remove the debugging check */
        }
    }
}