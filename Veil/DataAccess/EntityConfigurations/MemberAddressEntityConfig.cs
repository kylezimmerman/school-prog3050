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
             * Province: (ProvinceCode, CountryCode)
             * Country: CountryCode
             * Member: MemberId
             */

            HasRequired(ma => ma.Province).
                WithMany().
                HasForeignKey(
                    ma => new
                    {
                        ma.ProvinceCode,
                        ma.CountryCode
                    });

            HasRequired(ma => ma.Country).
                WithMany().
                HasForeignKey(ma => ma.CountryCode);

            HasRequired(ma => ma.Member).
                WithMany(m => m.ShippingAddresses).
                HasForeignKey(ma => ma.MemberId).
                WillCascadeOnDelete(true); // TODO: Figure out what this cascade delete actually means
        }
    }
}