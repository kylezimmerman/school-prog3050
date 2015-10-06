/* Employee.cs
 * Purpose: Class for employee and employee account information
 * 
 * Revision History:
 *      Drew Matheson, 2015.10.02: Created
 */

using System;
using System.ComponentModel.DataAnnotations;

namespace Veil.DataModels.Models
{
    public class Employee : Person
    {
        /// <summary>
        /// The employee's internal Id number
        /// </summary>
        [Required] // TODO: Do we want this to be required? Or do we leave it optional and generate an ID if it isn't provided
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