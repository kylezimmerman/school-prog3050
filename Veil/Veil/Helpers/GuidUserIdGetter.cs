using System;
using System.Security.Principal;
using Microsoft.AspNet.Identity;

namespace Veil.Helpers
{
    class GuidUserIdGetter : IGuidUserIdGetter {
        public Guid GetUserId(IIdentity userIdentity)
        {
            Guid result = Guid.Empty;
            string id = IdentityExtensions.GetUserId(userIdentity);
            Guid.TryParse(id, out result);

            return result;
        }
    }
}