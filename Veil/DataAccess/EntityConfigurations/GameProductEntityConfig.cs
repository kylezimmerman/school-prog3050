/* GameProductEntityConfig.cs
 * Purpose: Entity Type Configuration for the GameProduct model
 * 
 * Revision History:
 *      Drew Matheson, 2015.10.23: Created
 */

using System.Data.Entity.ModelConfiguration;
using Veil.DataModels.Models;

namespace Veil.DataAccess.EntityConfigurations
{
    /// <summary>
    ///     <see cref="EntityTypeConfiguration{T}"/> for the <see cref="GameProduct"/> model
    /// </summary>
    internal class GameProductEntityConfig : EntityTypeConfiguration<GameProduct>
    {
        public GameProductEntityConfig()
        {
            /* Foreign keys:
             *
             * Platform: PlatformCode
             * Game: GameId
             * GameProducts: PublisherId
             * GameProducts: DeveloperId
             */

            HasRequired(gp => gp.Platform).
                WithMany(p => p.GameProducts).
                HasForeignKey(gp => gp.PlatformCode);

            HasRequired(gp => gp.Game).
                WithMany(g => g.GameSKUs).
                HasForeignKey(gp => gp.GameId);

            HasRequired(gp => gp.Developer).
                WithMany(d => d.DevelopedGameProducts).
                HasForeignKey(gp => gp.DeveloperId);

            HasRequired(gp => gp.Publisher).
                WithMany(p => p.PublishedGameProducts).
                HasForeignKey(gp => gp.PublisherId);

            // Table per type for Products
            ToTable(nameof(GameProduct));
        }
    }
}