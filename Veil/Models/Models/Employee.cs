/* Employee.cs
 * Purpose: Class for employee information
 * 
 * Revision History:
 *      Drew Matheson, 2015.10.02: Created
 */

using System;
using System.ComponentModel.DataAnnotations;
using Veil.DataModels.Models.Identity;

namespace Veil.DataModels.Models
{
    public class Employee
    {
        /// <summary>
        /// The Id for the Employee's User account
        /// </summary>
        [Key]
        public Guid UserId { get; set; }

        /// <summary>
        /// Navigation property for the Employee's User account
        /// </summary>
        public virtual User UserAccount { get; set; }

        /// <summary>
        /// The employee's internal Id number
        /// </summary>
        [Required]
        public int EmployeeId { get; set; }

        /// <summary>
        /// The Id for the store location where the employee works
        /// </summary>
        public Guid StoreLocationId { get; set; }

        /// <summary>
        /// Navigation property for the employee's store location
        /// </summary>
        public virtual Location StoreLocation { get; set; }

        /// <summary>
        /// The Id for the department the employee is in
        /// </summary>
        [Required]
        public int DepartmentId { get; set; }

        /// <summary>
        /// Navigation property for the employee's department
        /// </summary>
        public virtual Department Department { get; set; }

        /// <summary>
        /// The date the employee was hired
        /// </summary>
        [Required]
        public DateTime HireDate { get; set; }

        /// <summary>
        /// The date the employee was terminated.
        /// This will be null if they are still employed
        /// </summary>
        public DateTime? TerminationDate { get; set; }
    }
}