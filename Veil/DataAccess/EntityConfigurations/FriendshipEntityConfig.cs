/* FriendshipEntityConfig.cs
 * Purpose: Entity Type Configuration for the Friendship model
 * 
 * Revision History:
 *      Drew Matheson, 2015.10.23: Created
 */

using System.Data.Entity.ModelConfiguration;
using Veil.DataModels.Models;

namespace Veil.DataAccess.EntityConfigurations
{
    /// <summary>
    ///     <see cref="EntityTypeConfiguration{T}"/> for the <see cref="Friendship"/> model
    /// </summary>
    internal class FriendshipEntityConfig : EntityTypeConfiguration<Friendship>
    {
        public FriendshipEntityConfig()
        {
            /* Primary Key:
             *
             * Member: ReceiverId
             * Member: RequesterId
             */
            HasKey(
                f => new
                {
                    f.ReceiverId,
                    f.RequesterId
                });

            /* Foreign Keys:
             *
             * Member: RequesterId
             * Member: ReceiverId
             */

            HasRequired(f => f.Requester).
                WithMany(m => m.RequestedFriendships).
                HasForeignKey(f => f.RequesterId);

            HasRequired(f => f.Receiver).
                WithMany(m => m.ReceivedFriendships).
                HasForeignKey(f => f.ReceiverId);
        }
    }
}