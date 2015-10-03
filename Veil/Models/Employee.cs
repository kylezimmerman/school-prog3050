/* Employee.cs
 * Purpose: Class for employee and employee account information
 * 
 * Revision History:
 *      Drew Matheson, 2015.10.02: Created
 */ 

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Veil.Models
{
    public class Employee : Person
    {
        /// <summary>
        /// The employee's internal Id number
        /// </summary>
        [Required]
        public int EmployeeId { get; set; }

        /// <summary>
        /// The Id for the store location where the employee works
        /// </summary>
        [Key]
        public Guid StoreLocationId { get; set; }

        /// <summary>
        /// Navigation property for the employee's store location
        /// </summary>
        [ForeignKey(nameof(StoreLocationId))]
        public virtual Location StoreLocation { get; set; }

        /// <summary>
        /// The Id for the department the employee is in
        /// </summary>
        [Required]
        public int DepartmentId { get; set; }

        /// <summary>
        /// Navigation property for the employee's department
        /// </summary>
        [ForeignKey(nameof(DepartmentId))]
        public virtual Department Department { get; set; }

        /// <summary>
        /// The date the employee was hired
        /// </summary>
        public DateTime HireDate { get; set; }

        /// <summary>
        /// The date the employee was terminated.
        /// This will be null if they are still employed
        /// </summary>
        public DateTime? TerminationDate { get; set; }
    }
}