/* Person.cs
 * Purpose: A base class for personal information
 * 
 * Revision History:
 *      Drew Matheson, 2015.10.03: Created
 */

using System;
using System.ComponentModel.DataAnnotations;

namespace Veil.DataModels.Models
{
    /// <summary>
    /// Base class for information about a person
    /// </summary>
    public class Person // TODO: Move into our identity class?
    {
        /// <summary>
        /// The Id for the Person
        /// </summary>
        [Key]
        public Guid PersonId { get; set; }

        /// <summary>
        /// The Person's first name
        /// </summary>
        public string FirstName { get; set; }

        /// <summary>
        /// The Person's last name
        /// </summary>
        public string LastName { get; set; }

        /// <summary>
        /// The Person's primary phone number
        /// </summary>
        [DataType(DataType.PhoneNumber)]
        public string PhoneNumber { get; set; }

        /// <summary>
        /// The Person's email address
        /// </summary>
        [DataType(DataType.EmailAddress)]
        public string Email { get; set; }
    }
}