/* OrderItemEntityConfig.cs
 * Purpose: Entity Type Configuration for the OrderItem model
 * 
 * Revision History:
 *      Drew Matheson, 2015.10.23: Created
 */

using System.Data.Entity.ModelConfiguration;
using Veil.DataModels.Models;

namespace Veil.DataAccess.EntityConfigurations
{
    internal class OrderItemEntityConfig : EntityTypeConfiguration<OrderItem>
    {
        public OrderItemEntityConfig()
        {
            /* Primary Key:
             *
             * OrderId, ProductId
             */
            HasKey(
                ci => new
                {
                    ci.OrderId,
                    ci.ProductId
                });

            /* Foreign Key:
             *
             * Product: ProductId
             * WebOrder: OrderId (setup in SetupWebOrderModel as OrderItem doesn't have
             *                    a navigation property to the WebOrder)
             */
            HasRequired(ci => ci.Product).
                WithMany().
                HasForeignKey(ci => ci.ProductId);
        }
    }
}