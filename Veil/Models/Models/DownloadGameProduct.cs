/* DownloadGameProduct.cs
 * Purpose: Class for a downloadable demo/shareware game
 * 
 * Revision History:
 *      Drew Matheson, 2015.10.02: Created
 */

using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Veil.DataModels.Validation;

namespace Veil.DataModels.Models
{
    /// <summary>
    /// A downloadable game product
    /// </summary>
    public class DownloadGameProduct : GameProduct
    {
        /// <summary>
        /// The game's download url
        /// </summary>
        [Required]
        [DataType(DataType.Url)]
        [Url]
        [StringLength(2048, ErrorMessageResourceName = nameof(ErrorMessages.StringLength), ErrorMessageResourceType = typeof(ErrorMessages))]
        [DisplayName("Download Link")]
        public string DownloadLink { get; set; }

        /// <summary>
        /// An approximate size in megabytes for the download
        /// </summary>
        [Range(0, int.MaxValue, ErrorMessageResourceName = nameof(ErrorMessages.Range), ErrorMessageResourceType = typeof(ErrorMessages))]
        [DisplayName("Approximate Size")]
        public int ApproximateSizeInMB { get; set; }
    }
}