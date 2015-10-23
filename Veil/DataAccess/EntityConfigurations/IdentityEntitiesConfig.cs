/* IdentityEntitiesConfig.cs
 * Purpose: Entity Configuration for the ASP.NET Identity models excluding User
 *          These aren't EntityTypeConfiguration classes because Identity override the values
 *          if they are.
 * 
 * Revision History:
 *      Drew Matheson, 2015.10.23: Created
 */

using System.Data.Entity;
using Veil.DataModels.Models.Identity;

namespace Veil.DataAccess.EntityConfigurations
{
    internal class IdentityEntitiesConfig
    {
        public static void Setup(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<GuidIdentityUserRole>().ToTable("UserRole");
            modelBuilder.Entity<GuidIdentityUserClaim>().ToTable("UserClaim");
            modelBuilder.Entity<GuidIdentityRole>().ToTable("Role");
            modelBuilder.Entity<GuidIdentityUserLogin>().ToTable("UserLogin");
        }
    }
}