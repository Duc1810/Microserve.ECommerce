namespace Cart.Domain.Entities
{
    public class ShoppingCart
    {
        public Guid Id { get; set; }
        public string UserName { get; set; } = string.Empty;
        public ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
        public decimal TotalPrice => CartItems.Sum(x => x.Price * x.Quantity);
        
    }
}
 