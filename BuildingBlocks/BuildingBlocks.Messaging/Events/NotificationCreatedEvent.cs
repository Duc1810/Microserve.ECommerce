using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildingBlocks.Messaging.Events
{
    public class CreatedEvent : IntegrationEvent
    {
        public Guid UserId { get; set; }
        public string Email { get; set; }
        public string Title { get; set; } = default!;
        public string Message { get; set; } = default!;
        public string? Href { get; set; } = "http://localhost:5151/hehe";
        public string FullName { get; set; } = default!;
        public Guid OrderId { get; set; }
        public decimal Price { get; set; }
        public int TotalItem { get; set; }

        public List<CreatedEventItem> Items { get; set; } = new();
    }
    public class CreatedEventItem
    {
        public Guid ProductId { get; set; }
        public int Quantity { get; set; }
    }
}
