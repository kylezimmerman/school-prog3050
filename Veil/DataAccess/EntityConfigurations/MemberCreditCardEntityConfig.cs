/* MemberCreditCardEntityConfig.cs
 * Purpose: Entity Type Configuration for the MemberCreditCard model
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
    ///     <see cref="EntityTypeConfiguration{T}"/> for the <see cref="MemberCreditCard"/> model
    /// </summary>
    internal class MemberCreditCardEntityConfig : EntityTypeConfiguration<MemberCreditCard>
    {
        public MemberCreditCardEntityConfig()
        {
            // TODO: StripeCardId should be made case sensitive in the DB

            /* Primary Key:
             *
             * Id
             */
            Property(cc => cc.Id).
                HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity);

            /* Foreign Keys:
             *
             * Member: MemberId
             */
            HasRequired(cc => cc.Member).
                WithMany(m => m.CreditCards).
                HasForeignKey(cc => cc.MemberId).
                WillCascadeOnDelete(true);
        }
    }
}