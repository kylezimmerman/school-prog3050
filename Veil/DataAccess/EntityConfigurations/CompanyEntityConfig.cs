/* CompanyEntityConfig.cs
 * Purpose: Entity Type Configuration for the Company model
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
    ///     <see cref="EntityTypeConfiguration{T}"/> for the <see cref="Company"/> model
    /// </summary>
    internal class CompanyEntityConfig : EntityTypeConfiguration<Company>
    {
        public CompanyEntityConfig()
        {
            Property(c => c.Id).
                HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity);
        }
    }
}