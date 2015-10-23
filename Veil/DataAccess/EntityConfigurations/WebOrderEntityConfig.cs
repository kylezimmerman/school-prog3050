/* WebOrderEntityConfig.cs
 * Purpose: Entity Type Configuration for the WebOrder model
 * 
 * Revision History:
 *      Drew Matheson, 2015.10.23: Created
 */

using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;
using Veil.DataModels.Models;

namespace Veil.DataAccess.EntityConfigurations
{
    internal class WebOrderEntityConfig : EntityTypeConfiguration<WebOrder>
    {
        public WebOrderEntityConfig()
        {
            // TODO: StripeChargeId should be case sensitive in the DB
            // TODO: StripeCardId should be case sensitive in the DB

            /* Primary Key:
             *
             * Id
             */

            HasKey(wo => wo.Id).
                Property(wo => wo.Id).
                HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity);

            /* Foreign keys:
             *
             * Member: MemberId
             * MemberAddress (ShippingAddress property): ShippingAddressId
             * MemberCreditCard: MemberCreditCardId
             */

            HasRequired(wo => wo.Member).
                WithMany(m => m.WebOrders).
                HasForeignKey(wo => wo.MemberId);

            HasRequired(wo => wo.ShippingAddress).
                WithMany().
                HasForeignKey(wo => wo.ShippingAddressId);

            HasRequired(wo => wo.MemberCreditCard).
                WithMany().
                HasForeignKey(wo => wo.MemberCreditCardId);

            HasMany(wo => wo.OrderItems).
                WithRequired().
                HasForeignKey(oi => oi.OrderId);
        }
    }
}