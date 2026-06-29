using BuildingBlocks.Observability.BaseEntity;

namespace Dashboard.Domain.Entities;

public class TopProductSnapshot : Entity<Guid>
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int TotalQuantitySold { get; set; }
    public decimal TotalRevenue { get; set; }
    public DateTime LastSoldAt { get; set; }
}