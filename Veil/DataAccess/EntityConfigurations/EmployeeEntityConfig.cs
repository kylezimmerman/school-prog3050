/* EmployeeEntityConfig.cs
 * Purpose: Entity Type Configuration for the Employee model
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
    internal class EmployeeEntityConfig : EntityTypeConfiguration<Employee>
    {
        public EmployeeEntityConfig()
        {
            /* Primary Key:
             *
             * UserId (mapped as EmployeeUserId)
             */

            HasKey(emp => emp.UserId).
                Property(emp => emp.UserId).
                HasColumnName("EmployeeUserId").
                HasDatabaseGeneratedOption(DatabaseGeneratedOption.None);

            /* Foreign Keys:
             *
             * User: UserId
             * Location: StoreLocationId
             * Department: DepartmentId
             */

            HasRequired(emp => emp.UserAccount).
                WithOptional(au => au.Employee);

            HasRequired(emp => emp.StoreLocation).
                WithMany().
                HasForeignKey(emp => emp.StoreLocationId);

            HasRequired(emp => emp.Department).
                WithMany().
                HasForeignKey(emp => emp.DepartmentId);

            // Unique constraint on the employee's Id
            Property(emp => emp.EmployeeId).
                HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity).
                HasColumnAnnotation(
                    IndexAnnotation.AnnotationName,
                    new IndexAnnotation(
                        new IndexAttribute("Employee_IX_EmployeeId_UQ")
                        {
                            IsUnique = true
                        }));

            ToTable(nameof(Employee));
        }
    }
}