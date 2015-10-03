/* ESRBContentDescriptor.cs
 * Purpose: Class for ESRB content descriptors (e.g. blood, violence, strong language)
 * 
 * Revision History:
 *      Drew Matheson, 2015.10.02: Created
 */ 

using System.ComponentModel.DataAnnotations;

namespace Veil.Models
{
    /// <summary>
    /// An ESRB content descriptor
    /// </summary>
    public class ESRBContentDescriptor
    {
        /// <summary>
        /// The Id for the content descriptor
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// The content descriptor's name
        /// </summary>
        public string DescriptorName { get; set; }
    }
}