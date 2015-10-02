using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Veil.Models
{
    public class Employee : Person // Table per concrete type
    {
        [Required]
        public int EmployeeId { get; set; }

        [Key]
        public Guid StoreLocationId { get; set; }

        [ForeignKey(nameof(StoreLocationId))]
        public virtual Location StoreLocation { get; set; }

        [Required]
        public int DepartmentId { get; set; }

        [ForeignKey(nameof(DepartmentId))]
        public virtual Department Department { get; set; }

        public DateTime HireDate { get; set; }

        public DateTime? TerminationDate { get; set; }
    }
}