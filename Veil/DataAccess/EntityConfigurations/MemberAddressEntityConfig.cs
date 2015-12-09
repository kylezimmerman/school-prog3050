/* MemberAddressEntityConfig.cs
 * Purpose: Entity Type Configuration for the MemberAddress model
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
    ///     <see cref="EntityTypeConfiguration{T}"/> for the <see cref="MemberAddress"/> model
    /// </summary>
    internal class MemberAddressEntityConfig : EntityTypeConfiguration<MemberAddress>
    {
        public MemberAddressEntityConfig()
        {
            /* Primary Key:
             *
             * Id
             */
            HasKey(ma => ma.Id).
                Property(ma => ma.Id).
                HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity);

            /* Foreign keys: 
             *
             * Country: Address.CountryCode
             * Province: (Address.ProvinceCode, Address.CountryCode)
             * Member: MemberId
             */

            HasRequired(ma => ma.Province).
                WithMany().
                HasForeignKey(ma => new { ma.ProvinceCode, ma.CountryCode });

            HasRequired(ma => ma.Country).
                WithMany().
                HasForeignKey(ma => ma.CountryCode);

            HasRequired(ma => ma.Member).
                WithMany(m => m.ShippingAddresses).
                HasForeignKey(ma => ma.MemberId).
                WillCascadeOnDelete(true);

            /* Map Complex Type */
            Property(wo => wo.Address.StreetAddress).HasColumnName(nameof(Address.StreetAddress));
            Property(wo => wo.Address.POBoxNumber).HasColumnName(nameof(Address.POBoxNumber));
            Property(wo => wo.Address.City).HasColumnName(nameof(Address.City));
            Property(wo => wo.Address.PostalCode).HasColumnName(nameof(Address.PostalCode));
        }
    }
}