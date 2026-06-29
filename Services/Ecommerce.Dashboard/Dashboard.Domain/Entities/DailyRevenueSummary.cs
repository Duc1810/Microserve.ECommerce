using BuildingBlocks.Observability.BaseEntity;

namespace Dashboard.Domain.Entities;

public class DailyRevenueSummary : Entity<Guid>
{
    public DateTime Date { get; set; }
    public decimal TotalRevenue { get; set; }
    public int CompletedOrderCount { get; set; }
    public int CancelledOrderCount { get; set; }
}