using Veil.DataModels.Models;

namespace Veil.Models
{
    public class PhysicalGameProductViewModel
    {
        public PhysicalGameProduct GameProduct { get; set; }
        public bool NewIsInCart { get; set; }
        public bool UsedIsInCart { get; set; }
        public bool ProductIsOnWishlist { get; set; }
    }
}