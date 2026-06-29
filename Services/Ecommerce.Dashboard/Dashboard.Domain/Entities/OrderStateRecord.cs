using BuildingBlocks.Observability.BaseEntity;

namespace Dashboard.Domain.Entities;

public class OrderStateRecord : Entity<Guid>
{
    // Id = OrderId (inherited from Entity<Guid>)
    public string Status { get; set; } = string.Empty;  // "Draft" | "Pending" | "Completed" | "Cancelled"
    public DateTime LastUpdatedAt { get; set; }
}