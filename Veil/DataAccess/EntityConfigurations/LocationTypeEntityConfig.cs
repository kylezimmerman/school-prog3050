/* LocationTypeEntityConfig.cs
 * Purpose: Entity Type Configuration for the LocationType model
 * 
 * Revision History:
 *      Drew Matheson, 2015.10.23: Created
 */

using System.Data.Entity.ModelConfiguration;
using Veil.DataModels.Models;

namespace Veil.DataAccess.EntityConfigurations
{
    internal class LocationTypeEntityConfig : EntityTypeConfiguration<LocationType>
    {
        public LocationTypeEntityConfig()
        {
            HasMany(lt => lt.Locations).
                WithRequired().
                HasForeignKey(l => l.LocationTypeName);
        }
    }
}