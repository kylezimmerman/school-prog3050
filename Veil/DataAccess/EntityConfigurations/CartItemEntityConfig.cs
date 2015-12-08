/* CartItemEntityConfig.cs
 * Purpose: Entity Type Configuration for the CartItem model
 * 
 * Revision History:
 *      Drew Matheson, 2015.10.23: Created
 */

using System.Data.Entity.ModelConfiguration;
using Veil.DataModels.Models;

namespace Veil.DataAccess.EntityConfigurations
{
    /// <summary>
    ///     <see cref="EntityTypeConfiguration{T}"/> for the <see cref="CartItem"/> model
    /// </summary>
    internal class CartItemEntityConfig : EntityTypeConfiguration<CartItem>
    {
        public CartItemEntityConfig()
        {
            /* Primary Key:
             *
             * MemberId (acts as PK for the cart), ProductId
             */

            HasKey(
                ci => new
                {
                    ci.MemberId,
                    ci.ProductId,
                    ci.IsNew
                });

            /* Foreign Keys:
             *
             * Product: ProductId
             * Cart: MemberId (setup in SetupCartModel)
             */
            HasRequired(ci => ci.Product).
                WithMany().
                HasForeignKey(ci => ci.ProductId);
        }
    }
}