/* MemberEntityConfig.cs
 * Purpose: Entity Type Configuration for the Member model
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
    ///     <see cref="EntityTypeConfiguration{T}"/> for the <see cref="Member"/> model
    /// </summary>
    internal class MemberEntityConfig : EntityTypeConfiguration<Member>
    {
        public MemberEntityConfig()
        {
            // TODO: StripeCustomerId should be case sensitive in the DB

            /* Primary Key:
             *
             * UserId
             */
            HasKey(m => m.UserId).
                Property(m => m.UserId).
                HasDatabaseGeneratedOption(DatabaseGeneratedOption.None);

            /* Foreign Keys:
             *
             * User: UserId
             */
            HasRequired(m => m.UserAccount).
                WithOptional(au => au.Member);

            /* Many to Many relationships:
             *
             * Member <=> Platform
             * Member <=> Tag
             * Member <=> Product (Wishlist)
             * Member <=> Event
             */

            HasMany(m => m.FavoritePlatforms).
                WithMany(p => p.MembersFavoritePlatform).
                Map(
                    manyToManyConfig =>
                        manyToManyConfig.ToTable("MemberFavoritePlatform"));

            HasMany(m => m.FavoriteTags).
                WithMany(t => t.MemberFavoriteCategory).
                Map(
                    manyToManyConfig =>
                        manyToManyConfig.ToTable("MemberFavoriteTag"));

            HasMany(m => m.Wishlist).
                WithMany().
                Map(
                    t =>
                        t.MapLeftKey("MemberId").
                        MapRightKey("ProductId").
                        ToTable("MemberWishlistItem"));

            HasMany(m => m.RegisteredEvents).
                WithMany(e => e.RegisteredMembers).
                Map(
                    manyToManyConfig =>
                        manyToManyConfig.ToTable("MemberEventMembership"));

            ToTable(nameof(Member));
        }
    }
}