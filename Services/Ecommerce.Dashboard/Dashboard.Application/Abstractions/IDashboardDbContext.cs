using Dashboard.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Dashboard.Application.Abstractions;

public interface IDashboardDbContext
{
    DbSet<DailyRevenueSummary> DailyRevenueSummaries { get; }
    DbSet<OrderStateRecord> OrderStateRecords { get; }
    DbSet<TopProductSnapshot> TopProductSnapshots { get; }
    DbSet<DashboardEventStore> DashboardEventStore { get; }
    
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}