/* CartEntityConfig.cs
 * Purpose: Entity Type Configuration for the Cart model
 * 
 * Revision History:
 *      Drew Matheson, 2015.10.23: Created
 */

using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;
using Veil.DataModels.Models;

namespace Veil.DataAccess.EntityConfigurations
{
    /// <summary>
    ///     <see cref="EntityTypeConfiguration{T}"/> for the <see cref="Cart"/> model
    /// </summary>
    internal class CartEntityConfig : EntityTypeConfiguration<Cart>
    {
        public CartEntityConfig()
        {
            /* Primary Key:
             *
             * MemberId
             */

            HasKey(c => c.MemberId).
                Property(c => c.MemberId).
                HasDatabaseGeneratedOption(DatabaseGeneratedOption.None);

            /* Foreign Key:
             *
             * Member: MemberId
             */

            HasRequired(c => c.Member).
                WithRequiredDependent(m => m.Cart);

            HasMany(c => c.Items).
                WithRequired().
                HasForeignKey(ci => ci.MemberId);

            ToTable(nameof(Member));
        }
    }
}