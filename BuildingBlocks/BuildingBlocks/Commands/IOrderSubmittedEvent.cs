
namespace BuildingBlocks.Commands
{
    public interface IOrderSubmittedEvent
    {
        Guid OrderId { get; }
        string UserId { get; }
        decimal TotalAmount { get; }
        List<OrderItemDto> Items { get; }
    }

    public class OrderItemDto
    {
        public string ProductId { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }
}
