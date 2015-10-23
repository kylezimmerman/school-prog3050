/* UserEntityConfig.cs
 * Purpose: Entity Configuration for the User model
 *          This isn't a EntityTypeConfiguration class because Identity override the values
 *          if they it is.
 * 
 * Revision History:
 *      Drew Matheson, 2015.10.23: Created
 */

using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using Veil.DataModels.Models.Identity;

namespace Veil.DataAccess.EntityConfigurations
{
    internal class UserEntityConfig
    {
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

            modelBuilder.Entity<User>().
                Property(u => u.Email).
                IsRequired();

            modelBuilder.Entity<User>().
                ToTable(nameof(User));
        }
    }
}