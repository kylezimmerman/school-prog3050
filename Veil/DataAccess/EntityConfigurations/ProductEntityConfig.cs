/* ProductEntityConfig.cs
 * Purpose: Entity Type Configuration for the Product model
 * 
 * Revision History:
 *      Drew Matheson, 2015.10.23: Created
 */

using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;
using Veil.DataModels.Models;

namespace Veil.DataAccess.EntityConfigurations
{
    internal class ProductEntityConfig : EntityTypeConfiguration<Product>
    {
        public ProductEntityConfig()
        {
            /* Primary Key:
             *
             * Id
             */
            HasKey(p => p.Id).
                Property(p => p.Id).
                HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity);

            /* Many to Many Relationships:
             *
             * Product <=> Tag
             */
            HasMany(p => p.Tags).
                WithMany(t => t.TaggedProducts).
                Map(manyToManyConfig => manyToManyConfig.ToTable("ProductCategory"));

            // Note: Table per type for Products
        }
    }
}