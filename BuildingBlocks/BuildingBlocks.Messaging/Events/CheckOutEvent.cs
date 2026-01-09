

namespace BuildingBlocks.Messaging.Events
{
    public class CheckOutEvent : IntegrationEvent
    {
        public string UserName { get; set; } = default!;
        public Guid CustomerId { get; set; } = default!;
        public decimal TotalPrice { get; set; } = default!;

        public string EmailAddress { get; set; } = default!;
        public string AddressLine { get; set; } = default!;
        public string State { get; set; } = default!;
        public string ZipCode { get; set; } = default!;


        public List<CheckOutItem> Items { get; set; } = new();
    }
    public record CheckOutItem(
       Guid ProductId,
       decimal Price,
       int Quantity
   );
}
