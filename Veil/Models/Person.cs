using System;
using System.ComponentModel.DataAnnotations;

namespace Veil.Models
{
    public class Person // TODO: Move into our identity class?
    {
        [Key]
        public Guid PersonId { get; set; }

        public string FirstName { get; set; }

        public string LastName { get; set; }

        public string PhoneNumber { get; set; }

        public string Email { get; set; }
    }
}