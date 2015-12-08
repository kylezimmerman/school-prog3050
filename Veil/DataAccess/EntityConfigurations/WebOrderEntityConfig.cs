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
    /// <summary>
    ///     <see cref="EntityTypeConfiguration{T}"/> for the <see cref="WebOrder"/> model
    /// </summary>
    internal class WebOrderEntityConfig : EntityTypeConfiguration<WebOrder>
    {
        public WebOrderEntityConfig()
        {
            // NOTE: StripeChargeId should be case sensitive in the DB

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
             * Country: Address.CountryCode
             * Province: (Address.ProvinceCode, Address.CountryCode)
             */

            HasRequired(wo => wo.Province).
                WithMany().
                HasForeignKey(wo => new
                {
                    wo.ProvinceCode,
                    wo.CountryCode
                });

            HasRequired(wo => wo.Country).
                WithMany().
                HasForeignKey(wo => wo.CountryCode);

            HasRequired(wo => wo.Member).
                WithMany(m => m.WebOrders).
                HasForeignKey(wo => wo.MemberId);

            HasMany(wo => wo.OrderItems).
                WithRequired().
                HasForeignKey(oi => oi.OrderId);

            /* Map Complex Type */
            Property(wo => wo.Address.StreetAddress).HasColumnName(nameof(Address.StreetAddress));
            Property(wo => wo.Address.POBoxNumber).HasColumnName(nameof(Address.POBoxNumber));
            Property(wo => wo.Address.City).HasColumnName(nameof(Address.City));
            Property(wo => wo.Address.PostalCode).HasColumnName(nameof(Address.PostalCode));
        }
    }
}