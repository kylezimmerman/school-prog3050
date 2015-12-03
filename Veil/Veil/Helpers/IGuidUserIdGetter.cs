/* IGuidUserIdGetter.cs
 * Purpose: Interface for getting an IIdentity's user Id as a guid. Allows easier testing
 * 
 * Revision History:
 *      Drew Matheson, 2015.12.03: Created
 */ 

using System;
using System.Security.Principal;

namespace Veil.Helpers
{
    /// <summary>
    ///     Interface containing a method for getting an <see cref="IIdentity"/>'s UserId as a
    ///     <see cref="Guid"/>
    /// </summary>
    public interface IGuidUserIdGetter
    {
        /// <summary>
        ///     Gets the identities Guid UserId
        /// </summary>
        /// <param name="userIdentity">
        ///     The <see cref="IIdentity"/> of the user
        /// </param>
        /// <returns>
        ///     The <see cref="Guid"/> UserId of the <see cref="IIdentity"/>
        /// </returns>
        Guid GetUserId(IIdentity userIdentity);
    }
}