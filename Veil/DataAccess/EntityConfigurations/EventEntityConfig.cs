/* CartEntityConfig.cs
 * Purpose: Entity Type Configuration for the Event model
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
    ///     <see cref="EntityTypeConfiguration{T}"/> for the <see cref="Event"/> model
    /// </summary>
    internal class EventEntityConfig : EntityTypeConfiguration<Event>
    {
        public EventEntityConfig()
        {
            /* Primary Key:
             *
             * Id
             */

            HasKey(c => c.Id).
                Property(c => c.Id).
                HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity);
        }
    }
}
