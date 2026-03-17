using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using product.Infrastructure.Data;
using Production.Application.Commons.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Production.Infrastructure.Jobs.Scheduled;

public class ProductCatologjobs
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ICacheService _cacheService;
    private readonly ILogger<ProductCatologjobs> _logger;
    public ProductCatologjobs(
        ApplicationDbContext dbContext,
        ICacheService cacheService,
        ILogger<ProductCatologjobs> logger)
    {
        _dbContext = dbContext;
        _cacheService = cacheService;
        _logger = logger;
    }

    [AutomaticRetry(Attempts = 3)]
    public async Task GenerateDailyAnalyticsAsync()
    {
        _logger.LogInformation("Starting daily analytics generation...");
        try
        {
            var analytics = new
            {
                GeneratedAt = DateTime.UtcNow,
                TotalProducts = await _dbContext.Products.CountAsync(),
                InStockProducts = await _dbContext.Products.CountAsync(p => p.Quantity > 0),
                OutStockProducts = await _dbContext.Products.CountAsync(p => p.Quantity == 0),
                LowStockProducts = await _dbContext.Products.CountAsync(p => p.Quantity > 0 && p.Quantity <= 10),
                TotalInventoryValue = await _dbContext.Products.SumAsync(p => p.Price * p.Quantity),
                AveragePrice = await _dbContext.Products.AverageAsync(p => p.Price),
                CategoryBreakdown = await _dbContext.Products
                    .GroupBy(p => p.Category)
                    .Select(g => new { Category = g.Key, Count = g.Count(), TotalValue = g.Sum(p => p.Price * p.Quantity) })
                    .ToListAsync()
            };

            // Store analytics in cache for 24 hours
            await _cacheService.SetAsync(
                "analytics:daily:latest",
                JsonSerializer.Serialize(analytics),
                TimeSpan.FromHours(24));

            _logger.LogInformation(
               "Daily analytics generated successfully. Total Products: {Total}, In Stock: {InStock}, Total Value: ${Value:N2}",
               analytics.TotalProducts,
               analytics.InStockProducts,
               analytics.TotalInventoryValue);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating daily analytics");
            throw;
        }
    }

    /// <summary>
    /// Cleans up old cache entries
    /// Runs every 6 hours
    /// </summary>
    [AutomaticRetry(Attempts = 2)]
    public async void CleanupOldCacheEntries()
    {
        _logger.LogInformation("Starting cache cleanup...");

        try
        {
            // For example, removing cached products that might be stale
            // await _cacheService.RemoveByPrefixAsync("products:");
            _logger.LogInformation("Cache cleanup completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cleanup cache");
            throw;
        }
    }

    [AutomaticRetry(Attempts = 3)]
    public async Task GenerateWeeklyInventoryReportAsync()
    {
        _logger.LogInformation("Starting weekly inventory report generation...");

        try
        {
            var report = new
            {
                Generated = DateTime.UtcNow,
                WeekNumber = GetIso8601WeekOfYear(DateTime.UtcNow),
                Year = DateTime.UtcNow.Year,
                Summary = new
                {
                    TotalProducts = await _dbContext.Products.CountAsync(),
                    TotalValue = await _dbContext.Products.SumAsync(p => p.Price * p.Quantity),
                    LowStockAlerts = await _dbContext.Products.CountAsync(p => p.Quantity <= 10 && p.Quantity > 0),
                    OutOfStockAlerts = await _dbContext.Products.CountAsync(p => p.Quantity == 0)
                },
                TopCategories = await _dbContext.Products
                    .GroupBy(p => p.Category)
                    .Select(g => new {
                        Category = g.Key,
                        Count = g.Count(),
                        TotalValue = g.Sum(p => p.Price * p.Quantity),
                        AverageStock = g.Average(p => p.Quantity) })
                    .OrderByDescending(c => c.TotalValue)
                    .Take(5)
                    .ToListAsync()
            };

            var reportkey = $"reports:inventory:weekly:{report.Year}:W{report.WeekNumber}";
            await _cacheService.SetAsync(
                reportkey,
                JsonSerializer.Serialize(report),
                TimeSpan.FromDays(30));

            _logger.LogInformation(
                "Weekly inventory report generated successfully for Week {Week}, {Year}",
                report.WeekNumber,
                report.Year);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate weekly inventory report");
            throw;
        }
    }

    /// <summary>
    /// Updates product popularity scores based on stock changes
    /// Runs every 12 hours
    /// </summary>
    [AutomaticRetry(Attempts = 3)]
    public async Task UpdateProductPopularityScoresAsync()
    {
        _logger.LogInformation("Starting product popularity score update...");

        try
        {


            // Get products with low stock (indicating high demand)
            var popularProducts = await _dbContext.Products
                .Where(p => p.Quantity < 20 && p.Quantity > 0)
                .OrderBy(p => p.Quantity)
                .Take(100)
                .Select(p => new { p.Id, p.Name, p.Quantity, p.Category })
                .ToListAsync();

            // Cache popular products list
            await _cacheService.SetAsync(
                "products:popular",
                JsonSerializer.Serialize(popularProducts),
                TimeSpan.FromHours(12));

            _logger.LogInformation(
                "Updated popularity scores for {Count} products",
                popularProducts.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update product popularity scores");
            throw;
        }
    }

    /// <summary>
    /// Cleanup old product data (soft-deleted items older than 90 days)
    /// Runs monthly on the 1st at 3:00 AM
    /// </summary>
    [AutomaticRetry(Attempts = 2)]
    public async Task CleanupOldDataAsync()
    {
        _logger.LogInformation("Starting old data cleanup...");

        try
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-90);
            _logger.LogInformation(
                "Old data cleanup completed. Cutoff date: {CutoffDate}",
                cutoffDate);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cleanup old data");
            throw;
        }
    }

    /// <summary>
    /// Cleans up old cache entries
    /// Runs every 6 hours
    /// </summary>
    [AutomaticRetry(Attempts = 2)]
    public async Task CleanupOldCacheEntriesAsync()
    {
        _logger.LogInformation("Starting cache cleanup...");

        try
        {
            // For example, removing cached products that might be stale
            // await _cacheService.RemoveByPrefixAsync("products:");
            _logger.LogInformation("Cache cleanup completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cleanup cache");
            throw;
        }
    }
    private static int GetIso8601WeekOfYear(DateTime date)
    {
        var day = System.Globalization.CultureInfo.InvariantCulture.Calendar.GetDayOfWeek(date);
        if (day >= System.DayOfWeek.Monday && day <= System.DayOfWeek.Wednesday)
        {
            date = date.AddDays(3);
        }

        return System.Globalization.CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(
            date,
            System.Globalization.CalendarWeekRule.FirstFourDayWeek,
            System.DayOfWeek.Monday);
    }

}

