using Dashboard.Application.Abstractions;
using Dashboard.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Dashboard.Infrastructure.Data;

public class DashboardDbContext : DbContext, IDashboardDbContext
{
    public DashboardDbContext(DbContextOptions<DashboardDbContext> options) : base(options)
    {
    }

    public DbSet<DailyRevenueSummary> DailyRevenueSummaries => Set<DailyRevenueSummary>();
    public DbSet<OrderStateRecord> OrderStateRecords => Set<OrderStateRecord>();
    public DbSet<TopProductSnapshot> TopProductSnapshots => Set<TopProductSnapshot>();
    public DbSet<DashboardEventStore> DashboardEventStore => Set<DashboardEventStore>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all entity configurations from this assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(DashboardDbContext).Assembly);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await base.SaveChangesAsync(cancellationToken);
    }
}