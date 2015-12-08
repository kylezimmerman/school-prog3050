/* LocationEntityConfig.cs
 * Purpose: Entity Type Configuration for the Location model
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
    ///     <see cref="EntityTypeConfiguration{T}"/> for the <see cref="Location"/> model
    /// </summary>
    internal class LocationEntityConfig : EntityTypeConfiguration<Location>
    {
        public LocationEntityConfig()
        {
            /* Primary Key:
             *
             * Id
             */
            HasKey(l => l.Id).
                Property(l => l.Id).
                HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity);

            /* Foreign keys: 
             *
             * Province: (ProvinceCode, CountryCode)
             * Country: CountryCode
             * LocationType: LocationTypeName (No Navigation property)
             */

            HasRequired(a => a.Province).
                WithMany().
                HasForeignKey(
                    a => new
                    {
                        a.ProvinceCode,
                        a.CountryCode
                    });

            HasRequired(a => a.Country).
                WithMany().
                HasForeignKey(a => a.CountryCode);
        }
    }
}