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

            // NOTE: This doesn't work as expected. It causes RequesterId to become a 1..1 with Member
            // TODO: Add unique constraint on (RequesterId, ReceiverId). 
            // PK already adds unique for (ReceiverId, RequesterId)
            //const string FRIENDSHIP_UNIQUE_INDEX = "Friendship_IX_Friendship_UQ";

            /*Property(f => f.RequesterId).
                HasColumnAnnotation(
                    IndexAnnotation.AnnotationName,
                    new IndexAnnotation(new IndexAttribute(FRIENDSHIP_UNIQUE_INDEX, 0)
                    {
                        IsUnique = true
                    }));

            
            Property(f => f.RequesterId).
                HasColumnAnnotation(
                    IndexAnnotation.AnnotationName,
                    new IndexAnnotation(new IndexAttribute(FRIENDSHIP_UNIQUE_INDEX, 1)
                    {
                        IsUnique = true
                    }));*/
        }
    }
}