/* GuidUserIdGetter.cs
 * Purpose: Implementation of IGuidUserIdGetter
 * 
 * Revision History:
 *      Drew Matheson, 2015.12.03: Created
 */ 

using System;
using System.Security.Principal;
using JetBrains.Annotations;
using Microsoft.AspNet.Identity;

namespace Veil.Helpers
{
    /// <summary>
    ///     Class which implements IGuidUserIdGetter
    /// </summary>
    [UsedImplicitly]
    internal class GuidUserIdGetter : IGuidUserIdGetter
    {
        /// <summary>
        ///     Implements <see cref="IGuidUserIdGetter.GetUserId(IIdentity)"/>
        /// </summary>
        public Guid GetUserId(IIdentity userIdentity)
        {
            Guid result;
            string id = IdentityExtensions.GetUserId(userIdentity);
            Guid.TryParse(id, out result);

            return result;
        }
    }
}