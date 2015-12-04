/* Department.cs
 * Purpose: Class to indicate which department an employee is in within the company
 * 
 * Revision History:
 *      Drew Matheson, 2015.10.02: Created
 */

using System.ComponentModel.DataAnnotations;
using Veil.DataModels.Validation;

namespace Veil.DataModels.Models
{
    /// <summary>
    /// A department within a company
    /// </summary>
    public class Department
    {
        /// <summary>
        /// The Id of the department
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// The department's name
        /// </summary>
        [Required]
        [StringLength(128, ErrorMessageResourceName = nameof(ErrorMessages.StringLength), ErrorMessageResourceType = typeof(ErrorMessages) )]
        public string Name { get; set; }
    }
}