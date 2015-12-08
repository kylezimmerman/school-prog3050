/* ProvinceEntityConfig.cs
 * Purpose: Entity Type Configuration for the Province model
 * 
 * Revision History:
 *      Drew Matheson, 2015.10.23: Created
 */

using System.Data.Entity.ModelConfiguration;
using Veil.DataModels.Models;

namespace Veil.DataAccess.EntityConfigurations
{
    /// <summary>
    ///     <see cref="EntityTypeConfiguration{T}"/> for the <see cref="Province"/> model
    /// </summary>
    internal class ProvinceEntityConfig : EntityTypeConfiguration<Province>
    {
        public ProvinceEntityConfig()
        {
            /* Primary key:
             * 
             * ProvinceCode, CountryCode
             */
            HasKey(
                p => new
                {
                    p.ProvinceCode,
                    p.CountryCode
                });

            /* Foreign keys:
             *
             * Country: CountryCode
             */
            HasRequired(p => p.Country).
                WithMany(c => c.Provinces).
                HasForeignKey(p => p.CountryCode);
        }
    }
}