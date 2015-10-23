/* ESRBRatingEntityConfig.cs
 * Purpose: Entity Type Configuration for the ESRBRating model
 * 
 * Revision History:
 *      Drew Matheson, 2015.10.23: Created
 */

using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;
using Veil.DataModels.Models;

namespace Veil.DataAccess.EntityConfigurations
{
    internal class ESRBRatingEntityConfig : EntityTypeConfiguration<ESRBRating>
    {
        public ESRBRatingEntityConfig()
        {
            Property(er => er.RatingId).
                HasDatabaseGeneratedOption(DatabaseGeneratedOption.None);
        }
    }
}