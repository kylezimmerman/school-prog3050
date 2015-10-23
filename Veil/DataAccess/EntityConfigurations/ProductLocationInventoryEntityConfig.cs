/* ProductLocationInventoryEntityConfig.cs
 * Purpose: Entity Type Configuration for the ProductLocationInventory model
 * 
 * Revision History:
 *      Drew Matheson, 2015.10.23: Created
 */

using System.Data.Entity.ModelConfiguration;
using Veil.DataModels.Models;

namespace Veil.DataAccess.EntityConfigurations
{
    internal class ProductLocationInventoryEntityConfig : EntityTypeConfiguration<ProductLocationInventory>
    {
        public ProductLocationInventoryEntityConfig()
        {
            /* Primary Key:
             *
             * LocationId, ProductId
             */

            HasKey(
                pli => new
                {
                    pli.LocationId,
                    pli.ProductId
                });

            /* Foreign keys:
             *
             * Product: ProductId
             * Location: LocationId
             */

            HasRequired(pli => pli.Product).
                WithMany(p => p.LocationInventories).
                HasForeignKey(pli => pli.ProductId);

            HasRequired(pli => pli.Location).
                WithMany().
                HasForeignKey(pli => pli.LocationId);
        }
    }
}