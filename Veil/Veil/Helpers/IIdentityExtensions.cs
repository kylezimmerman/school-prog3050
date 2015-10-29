using System;
using System.Security.Principal;
using Microsoft.AspNet.Identity;

namespace Veil.Helpers
{
    public static class IIdentityExtensions
    {
        public static Guid GetUserId(this IIdentity identity)
        {
            Guid result = Guid.Empty;
            string id = IdentityExtensions.GetUserId(identity);
            Guid.TryParse(id, out result);

            return result;
        }
    }
}