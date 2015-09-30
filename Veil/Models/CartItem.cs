namespace Veil.Models
{
    public class CartItem
    {
        public virtual Product Product { get; set; }

        public int Quantity { get; set; }
    }
}