/* DownloadGameProductEntityConfig.cs
 * Purpose: Entity Type Configuration for the DownloadGameProduct model
 * 
 * Revision History:
 *      Drew Matheson, 2015.10.23: Created
 */

using System.Data.Entity.ModelConfiguration;
using Veil.DataModels.Models;

namespace Veil.DataAccess.EntityConfigurations
{
    /// <summary>
    ///     <see cref="EntityTypeConfiguration{T}"/> for the <see cref="DownloadGameProduct"/> model
    /// </summary>
    internal class DownloadGameProductEntityConfig : EntityTypeConfiguration<DownloadGameProduct>
    {
        public DownloadGameProductEntityConfig()
        {
            // Table per type for Products
            ToTable(nameof(DownloadGameProduct));
        }
    }
}