/* UserEntityConfig.cs
 * Purpose: Entity Configuration for the User model
 *          This isn't a EntityTypeConfiguration class because Identity override the values
 *          if it is.
 * 
 * Revision History:
 *      Drew Matheson, 2015.10.23: Created
 */

using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using Veil.DataModels.Models.Identity;

namespace Veil.DataAccess.EntityConfigurations
{
    /// <summary>
    ///     Entity configuration for the <see cref="User"/> model.
    ///     <br/>
    ///     This isn't a EntityTypeConfiguration class because Identity overrides the values
    ///     if it is.
    /// </summary>
    internal class UserEntityConfig
    {
        /// <summary>
        ///     Sets up the entity model/configuration for the <see cref="User"/> model
        /// </summary>
        /// <param name="modelBuilder">
        ///     The <see cref="DbModelBuilder"/> to be configured for the <see cref="User"/> model
        /// </param>
        public static void Setup(DbModelBuilder modelBuilder)
        {
            /* Primary Key:
             *
             * Id
             */
            modelBuilder.Entity<User>().
                HasKey(u => u.Id).
                Property(u => u.Id).
                HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity);

            // Cascade delete the user's GuidIdentityUserRoles when the user is deleted
            modelBuilder.Entity<User>().
                HasMany(u => u.Roles).
                WithRequired().
                HasForeignKey(r => r.UserId).
                WillCascadeOnDelete(true);

            modelBuilder.Entity<User>().
                HasMany(u => u.Logins).
                WithRequired().
                HasForeignKey(l => l.UserId).
                WillCascadeOnDelete(true);

            modelBuilder.Entity<User>().
                HasMany(u => u.Claims).
                WithRequired().
                HasForeignKey(uc => uc.UserId).
                WillCascadeOnDelete(true);

            modelBuilder.Entity<User>().
                Property(u => u.Email).
                IsRequired();

            modelBuilder.Entity<User>().
                ToTable(nameof(User));
        }
    }
}