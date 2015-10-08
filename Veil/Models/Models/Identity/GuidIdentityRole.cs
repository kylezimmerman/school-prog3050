/* GuidIdentityRole.cs
 * Purpose: A custom IdentityRole with Guid as the key type
 * 
 * Revision History:
 *      Drew Matheson, 2015.10.08: Created
 */

using System;
using Microsoft.AspNet.Identity.EntityFramework;

namespace Veil.DataModels.Models.Identity
{
    /// <summary>
    /// IdentityRole with Guid as the key type and GuidIdentityUserRole as the role type
    /// <remarks>
    ///     This class is required by Entity Framework
    /// </remarks>
    /// </summary>
    public class GuidIdentityRole : IdentityRole<Guid, GuidIdentityUserRole> { }
}
