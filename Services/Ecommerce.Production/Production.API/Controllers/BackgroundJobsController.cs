using Hangfire;
using Microsoft.AspNetCore.Mvc;
using Production.Application.Commons.Interfaces;
using Production.Application.Dtos.Products;
using Production.Infrastructure.Jobs.Scheduled;
using System.Text.Json;
namespace Production.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BackgroundJobsController : ControllerBase
{
    private readonly ILogger<BackgroundJobsController> _logger;
    private readonly IBackgroundJobClient _backgroundClient;
    private readonly ICacheService _cacheService;

    public BackgroundJobsController(ILogger<BackgroundJobsController> logger, IBackgroundJobClient backgroundClient, ICacheService cacheService)
    {
        _logger = logger;
        _backgroundClient = backgroundClient;
        _cacheService = cacheService;
    }

    [HttpPost("trigger-analytics")]
    public IActionResult TriggerDailyAnalytics()
    {
        var jobId = _backgroundClient.Enqueue<ProductCatologjobs>(job => job.GenerateDailyAnalyticsAsync());
        _logger.LogInformation("Manually triggered daily analytics job.");
        return Ok(new
        {
            Message = "Daily analytics job has been triggered.",
            jobId = jobId,
            dashboardUrl = "/hangfire"
        });
    }

    /// <summary>
    /// Manually trigger inventory report generation
    /// </summary>
    [HttpPost("trigger-inventory-report")]
    public IActionResult TriggerInventoryReport()
    {
        var jobId = _backgroundClient.Enqueue<ProductCatologjobs>(
            job => job.GenerateWeeklyInventoryReportAsync());

        _logger.LogInformation("Manually triggered inventory report generation. Job ID: {JobId}", jobId);

        return Ok(new
        {
            message = "Inventory report generation job queued successfully",
            jobId = jobId,
            dashboardUrl = "/hangfire"
        });
    }

    [HttpGet("analitics")]
    public async Task<IActionResult> GetDailyAnalytics()
    {
        const string cacheKey = "analytics:daily:latest";

        var report = await _cacheService.GetAsync<object>(cacheKey);

        if (report == null)
        {
            return NotFound(new
            {
                Message = "Dữ liệu chưa được khởi tạo. Hãy đợi Job chạy hoặc bấm 'Trigger' trên Hangfire Dashboard.",
                LastUpdated = DateTime.Now
            });
        }

        return Ok(new
        {
            Title = "Báo cáo phân tích hệ thống",
            Source = "Redis Cache",
            Data = report
        });
    }

    /// <summary>
    /// Lấy danh sách sản phẩm hot dựa trên điểm số đã tính từ Hangfire
    /// </summary>
    [HttpGet("trending")]
    public async Task<IActionResult> GetTrendingProducts()
    {
        // 1. Cái Key này phải khớp với Key ông dùng trong hàm UpdateProductPopularityScoresAsync
        const string cacheKey = "products:popular";

        var jsonString = await _cacheService.GetAsync<string>(cacheKey);

        if (string.IsNullOrEmpty(jsonString)) return NotFound("No data");

        var trendingData = JsonSerializer.Deserialize<List<ProductPopularDto>>(jsonString, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        return Ok(trendingData);
    }


    /// <summary>
    /// Get information about recurring jobs
    /// </summary>
    [HttpGet("recurring-jobs")]
    public IActionResult GetRecurringJobs()
    {
        var recurringJobs = new[]
        {
            new
            {
                id = "daily-analytics",
                name = "Daily Analytics Generation",
                schedule = "Daily at 2:00 AM UTC",
                cron = "0 2 * * *"
            },
            new
            {
                id = "cache-cleanup",
                name = "Cache Cleanup",
                schedule = "Every 6 hours",
                cron = "0 */6 * * *"
            },
            new
            {
                id = "update-popularity-scores",
                name = "Product Popularity Update",
                schedule = "Every 12 hours",
                cron = "0 */12 * * *"
            },
            new
            {
                id = "weekly-inventory-report",
                name = "Weekly Inventory Report",
                schedule = "Every Monday at 8:00 AM UTC",
                cron = "0 8 * * 1"
            },
            new
            {
                id = "monthly-data-cleanup",
                name = "Monthly Data Cleanup",
                schedule = "1st of month at 3:00 AM UTC",
                cron = "0 3 1 * *"
            }
        };

        return Ok(new
        {
            recurringJobs = recurringJobs,
            dashboardUrl = "/hangfire",
            totalJobs = recurringJobs.Length
        });
    }
}

