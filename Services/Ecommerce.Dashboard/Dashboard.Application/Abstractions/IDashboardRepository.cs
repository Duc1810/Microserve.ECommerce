using Dashboard.Application.DTOs;
using Dashboard.Domain.Entities;

namespace Dashboard.Application.Abstractions;

public interface IDashboardRepository
{
    // Revenue Summary Operations
    Task<IEnumerable<DailyRevenueSummary>> GetRevenueSummaryAsync(DateTime fromDate, DateTime toDate);
    Task UpsertDailyRevenueSummaryAsync(DateTime date, decimal amount, int completedOrderCount = 1);

    // Order State Operations
    Task<IEnumerable<OrderStatusSummaryResult>> GetOrderStatusSummaryAsync();
    Task UpsertOrderStateAsync(Guid orderId, string status, DateTime lastUpdatedAt);

    // Top Products Operations
    Task<IEnumerable<TopProductItem>> GetTopProductsAsync(int topN);
    Task UpsertTopProductAsync(Guid productId, string productName, int quantity, decimal revenue, DateTime lastSoldAt);

    // EventStore Operations
    Task<int> InsertEventIfNotExistsAsync(Guid eventId, string eventType, string payload, DateTime occurredOn);
    Task<IEnumerable<DashboardEventStore>> GetAllEventsOrderedAsync();

    // Rebuild Operations
    Task TruncateAllMaterializedViewsAsync();

    // Revenue Time Series
    Task<IEnumerable<RevenueDataPoint>> GetRevenueTimeSeriesAsync(DateTime fromDate, DateTime toDate, string period);
}