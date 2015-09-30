using System;
using System.Collections.Generic;

namespace Veil.Models
{
    public class Event
    {
        public string Id { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public DateTime Date { get; set; }

        public virtual ICollection<Member> RegisteredMembers { get; set; } 
    }
}