/* PhysicalGameProductEntityConfig.cs
 * Purpose: Entity Type Configuration for the PhysicalGameProduct model
 * 
 * Revision History:
 *      Drew Matheson, 2015.10.23: Created
 */

using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.Infrastructure.Annotations;
using System.Data.Entity.ModelConfiguration;
using Veil.DataModels.Models;

namespace Veil.DataAccess.EntityConfigurations
{
    /// <summary>
    ///     <see cref="EntityTypeConfiguration{T}"/> for the <see cref="PhysicalGameProduct"/> model
    /// </summary>
    internal class PhysicalGameProductEntityConfig : EntityTypeConfiguration<PhysicalGameProduct>
    {
        public PhysicalGameProductEntityConfig()
        {
            /* PhysicalGameProduct Unique Constraints:
            *
            * InternalNewSKU 
            * InternalUsedSKU
            */

            Property(pgp => pgp.InternalNewSKU).HasColumnAnnotation(
                IndexAnnotation.AnnotationName,
                new IndexAnnotation(
                    new IndexAttribute("PhysicalGameProduct_IX_InternalNewSKU_UQ")
                    {
                        IsUnique = true
                    }));

            Property(pgp => pgp.InteralUsedSKU).HasColumnAnnotation(
                IndexAnnotation.AnnotationName,
                new IndexAnnotation(
                    new IndexAttribute("PhysicalGameProduct_IX_InternalUsedSKU_UQ")
                    {
                        IsUnique = true
                    }));

            // Table per type for Products
            ToTable(nameof(PhysicalGameProduct));
        }
    }
}