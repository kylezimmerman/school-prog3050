/* GameReviewEntityConfig.cs
 * Purpose: Entity Type Configuration for the GameReview model
 * 
 * Revision History:
 *      Drew Matheson, 2015.10.23: Created
 */

using System.Data.Entity.ModelConfiguration;
using Veil.DataModels.Models;

namespace Veil.DataAccess.EntityConfigurations
{
    internal class GameReviewEntityConfig : EntityTypeConfiguration<GameReview>
    {
        public GameReviewEntityConfig()
        {
            /* Primary Key:
             *
             * MemberId, GameProductId
             */

            HasKey(
                g => new
                {
                    g.MemberId,
                    g.ProductReviewedId
                });

            /* Foreign keys:
             *
             * GameProduct: GameProductId
             */

            HasRequired(gr => gr.ProductReviewed).
                WithMany(gp => gp.Reviews).
                HasForeignKey(gr => gr.ProductReviewedId).
                WillCascadeOnDelete(true);

            HasRequired(g => g.Member).
                WithMany().
                HasForeignKey(g => g.MemberId).
                WillCascadeOnDelete(true);

            // Map concrete type
            Map(
                t =>
                    t.MapInheritedProperties().
                    ToTable(nameof(GameReview)));
        }
    }
}