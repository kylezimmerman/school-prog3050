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
             * OrderId, ProductId, IsNew
             */
            HasKey(
                oi => new
                {
                    oi.OrderId,
                    oi.ProductId,
                    oi.IsNew
                });

            /* Foreign Key:
             *
             * Product: ProductId
             * WebOrder: OrderId (setup in SetupWebOrderModel as OrderItem doesn't have
             *                    a navigation property to the WebOrder)
             */
            HasRequired(oi => oi.Product).
                WithMany().
                HasForeignKey(oi => oi.ProductId);
        }
    }
}