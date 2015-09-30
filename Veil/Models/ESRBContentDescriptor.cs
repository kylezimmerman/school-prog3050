using System.ComponentModel.DataAnnotations;

namespace Veil.Models
{
    public class ESRBContentDescriptor
    {
        [Key]
        public int Id { get; set; }

        public string DescriptorName { get; set; }
    }
}