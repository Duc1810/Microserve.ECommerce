namespace Cart.Domain.Entities
{
    public class CartItem
    {
        public Guid Id { get; set; }
        public int Quantity { get; set; } 
        public string? Color { get; set; } 
        public decimal Price { get; set; } 
        public Guid ProductId { get; set; } 
        public string ProductName { get; set; } = string.Empty;

        public Guid CartId { get; set; }
        public ShoppingCart? Cart { get; set; }
    }
}
