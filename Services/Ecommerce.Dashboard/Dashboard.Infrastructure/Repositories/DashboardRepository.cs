using Dapper;
using Dashboard.Application.Abstractions;
using Dashboard.Application.DTOs;
using Dashboard.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace Dashboard.Infrastructure.Repositories;

public class DashboardRepository : IDashboardRepository
{
    private readonly string _connectionString;

    public DashboardRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection") 
            ?? throw new ArgumentNullException(nameof(configuration));
    }

    public async Task<IEnumerable<DailyRevenueSummary>> GetRevenueSummaryAsync(DateTime fromDate, DateTime toDate)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        
        const string sql = @"
            SELECT date, total_revenue as TotalRevenue, completed_order_count as CompletedOrderCount
            FROM daily_revenue_summary 
            WHERE date >= @FromDate AND date <= @ToDate
            ORDER BY date";

        return await connection.QueryAsync<DailyRevenueSummary>(sql, new { FromDate = fromDate, ToDate = toDate });
    }

    public async Task UpsertDailyRevenueSummaryAsync(DateTime date, decimal amount, int completedOrderCount = 1)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        
        const string sql = @"
            INSERT INTO daily_revenue_summary (date, total_revenue, completed_order_count, last_updated_at)
            VALUES (@Date, @Amount, @CompletedOrderCount, @LastUpdatedAt)
            ON CONFLICT (date) 
            DO UPDATE SET 
                total_revenue = daily_revenue_summary.total_revenue + @Amount,
                completed_order_count = daily_revenue_summary.completed_order_count + @CompletedOrderCount,
                last_updated_at = @LastUpdatedAt";

        await connection.ExecuteAsync(sql, new 
        { 
            Date = date.Date, 
            Amount = amount, 
            CompletedOrderCount = completedOrderCount,
            LastUpdatedAt = DateTime.UtcNow 
        });
    }

    public async Task<IEnumerable<OrderStatusSummaryResult>> GetOrderStatusSummaryAsync()
    {
        using var connection = new NpgsqlConnection(_connectionString);
        
        const string sql = @"
            SELECT 
                COALESCE(SUM(CASE WHEN status = 'Draft' THEN 1 ELSE 0 END), 0) as Draft,
                COALESCE(SUM(CASE WHEN status = 'Pending' THEN 1 ELSE 0 END), 0) as Pending,
                COALESCE(SUM(CASE WHEN status = 'Completed' THEN 1 ELSE 0 END), 0) as Completed,
                COALESCE(SUM(CASE WHEN status = 'Cancelled' THEN 1 ELSE 0 END), 0) as Cancelled,
                COUNT(*) as Total
            FROM order_state_summary";

        var result = await connection.QuerySingleAsync<OrderStatusSummaryResult>(sql);
        return new[] { result };
    }

    public async Task UpsertOrderStateAsync(Guid orderId, string status, DateTime lastUpdatedAt)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        
        const string sql = @"
            INSERT INTO order_state_summary (order_id, status, last_updated_at)
            VALUES (@OrderId, @Status, @LastUpdatedAt)
            ON CONFLICT (order_id) 
            DO UPDATE SET 
                status = @Status,
                last_updated_at = @LastUpdatedAt";

        await connection.ExecuteAsync(sql, new { OrderId = orderId, Status = status, LastUpdatedAt = lastUpdatedAt });
    }

    public async Task<IEnumerable<TopProductItem>> GetTopProductsAsync(int topN)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        
        const string sql = @"
            SELECT product_id as ProductId, product_name as ProductName, 
                   total_quantity as TotalQuantity, total_revenue as TotalRevenue,
                   last_sold_at as LastSoldAt
            FROM top_product_snapshot 
            ORDER BY total_revenue DESC 
            LIMIT @TopN";

        return await connection.QueryAsync<TopProductItem>(sql, new { TopN = topN });
    }

    public async Task UpsertTopProductAsync(Guid productId, string productName, int quantity, decimal revenue, DateTime lastSoldAt)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        
        const string sql = @"
            INSERT INTO top_product_snapshot (product_id, product_name, total_quantity, total_revenue, last_sold_at, last_updated_at)
            VALUES (@ProductId, @ProductName, @Quantity, @Revenue, @LastSoldAt, @LastUpdatedAt)
            ON CONFLICT (product_id) 
            DO UPDATE SET 
                product_name = @ProductName,
                total_quantity = top_product_snapshot.total_quantity + @Quantity,
                total_revenue = top_product_snapshot.total_revenue + @Revenue,
                last_sold_at = GREATEST(top_product_snapshot.last_sold_at, @LastSoldAt),
                last_updated_at = @LastUpdatedAt";

        await connection.ExecuteAsync(sql, new 
        { 
            ProductId = productId, 
            ProductName = productName, 
            Quantity = quantity, 
            Revenue = revenue, 
            LastSoldAt = lastSoldAt,
            LastUpdatedAt = DateTime.UtcNow 
        });
    }

    public async Task<int> InsertEventIfNotExistsAsync(Guid eventId, string eventType, string payload, DateTime occurredOn)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        
        const string sql = @"
            INSERT INTO dashboard_event_store (event_id, event_type, payload, occurred_on, processed_at)
            VALUES (@EventId, @EventType, @Payload, @OccurredOn, @ProcessedAt)
            ON CONFLICT (event_id) DO NOTHING";

        return await connection.ExecuteAsync(sql, new 
        { 
            EventId = eventId, 
            EventType = eventType, 
            Payload = payload, 
            OccurredOn = occurredOn,
            ProcessedAt = DateTime.UtcNow 
        });
    }

    public async Task<IEnumerable<DashboardEventStore>> GetAllEventsOrderedAsync()
    {
        using var connection = new NpgsqlConnection(_connectionString);
        
        const string sql = @"
            SELECT event_id as EventId, event_type as EventType, payload as Payload, 
                   occurred_on as OccurredOn, processed_at as ProcessedAt
            FROM dashboard_event_store 
            ORDER BY occurred_on";

        return await connection.QueryAsync<DashboardEventStore>(sql);
    }

    public async Task TruncateAllMaterializedViewsAsync()
    {
        using var connection = new NpgsqlConnection(_connectionString);
        
        const string sql = @"
            TRUNCATE TABLE daily_revenue_summary, order_state_summary, top_product_snapshot, dashboard_event_store";

        await connection.ExecuteAsync(sql);
    }

    public async Task<IEnumerable<RevenueDataPoint>> GetRevenueTimeSeriesAsync(DateTime fromDate, DateTime toDate, string period)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        
        string sql = period.ToLower() switch
        {
            "daily" => @"
                SELECT date as Date, total_revenue as Revenue
                FROM daily_revenue_summary 
                WHERE date >= @FromDate AND date <= @ToDate
                ORDER BY date",
            
            "weekly" => @"
                SELECT DATE_TRUNC('week', date) as Date, SUM(total_revenue) as Revenue
                FROM daily_revenue_summary 
                WHERE date >= @FromDate AND date <= @ToDate
                GROUP BY DATE_TRUNC('week', date)
                ORDER BY Date",
            
            "monthly" => @"
                SELECT DATE_TRUNC('month', date) as Date, SUM(total_revenue) as Revenue
                FROM daily_revenue_summary 
                WHERE date >= @FromDate AND date <= @ToDate
                GROUP BY DATE_TRUNC('month', date)
                ORDER BY Date",
            
            _ => throw new ArgumentException($"Unsupported period: {period}")
        };

        return await connection.QueryAsync<RevenueDataPoint>(sql, new { FromDate = fromDate, ToDate = toDate });
    }
}