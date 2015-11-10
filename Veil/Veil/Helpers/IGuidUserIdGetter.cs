using System;
using System.Security.Principal;

namespace Veil.Helpers
{
    public interface IGuidUserIdGetter
    {
        Guid GetUserId(IIdentity userIdentity);
    }
}
