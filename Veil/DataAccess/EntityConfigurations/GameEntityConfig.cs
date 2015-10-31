/* GameEntityConfig.cs
 * Purpose: Entity Type Configuration for the Game model
 * 
 * Revision History:
 *      Drew Matheson, 2015.10.23: Created
 */

using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;
using Veil.DataModels.Models;

namespace Veil.DataAccess.EntityConfigurations
{
    internal class GameEntityConfig : EntityTypeConfiguration<Game>
    {
        public GameEntityConfig()
        {
            /* Primary Key:
             *
             * Id
             */
            HasKey(g => g.Id).
                Property(g => g.Id).
                HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity);

            /* Foreign Keys:
             *
             * ESRBRating: ESRBRatingId
             */

            HasRequired(g => g.Rating).
                WithMany(r => r.Games).
                HasForeignKey(g => g.ESRBRatingId);

            /* Many to Many Relationships:
             *
             * Game <=> ESRBContentDescriptors
             * Game <=> Tags
             */

            HasMany(g => g.ContentDescriptors).
                WithMany();

            HasMany(p => p.Tags).
                WithMany(t => t.TaggedGames).
                Map(manyToManyConfig => manyToManyConfig.ToTable("GameCategory"));
        }
    }
}