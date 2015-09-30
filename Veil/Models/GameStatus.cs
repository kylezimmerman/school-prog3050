using System.ComponentModel.DataAnnotations;

namespace Veil.Models
{
    public class GameStatus
    {
        [Key]
        public int Id { get; set; }

        public string Category { get; set; }
    }
}