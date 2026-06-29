using Dashboard.Application.Queries;
using Dashboard.Application.Commands;
using Dashboard.Domain.Enums;
using Dashboard.Domain.ValueObjects;
using BuildingBlocks.Observability.ApiResponse;

namespace Dashboard.API.Controllers;

[ApiController]
[Route("api/dashboard")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(IMediator mediator, ILogger<DashboardController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Get revenue summary for a date range
    /// </summary>
    [HttpGet("revenue/summary")]
    public async Task<IActionResult> GetRevenueSummary(
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        CancellationToken cancellationToken = default)
    {
        if (!from.HasValue || !to.HasValue)
        {
            return BadRequest(ApiResponse<object>.Fail("Both 'from' and 'to' parameters are required"));
        }

        if (from.Value > to.Value)
        {
            return BadRequest(ApiResponse<object>.Fail("'from' date must be less than or equal to 'to' date"));
        }

        var query = new GetRevenueSummaryQuery(new DateRangeFilter(from.Value, to.Value));
        var result = await _mediator.Send(query, cancellationToken);
        
        return Ok(ApiResponse<RevenueSummaryResult>.Ok(result));
    }

    /// <summary>
    /// Get current order status distribution
    /// </summary>
    [HttpGet("orders/status")]
    public async Task<IActionResult> GetOrderStatusSummary(CancellationToken cancellationToken = default)
    {
        var query = new GetOrderStatusSummaryQuery();
        var result = await _mediator.Send(query, cancellationToken);
        
        return Ok(ApiResponse<OrderStatusSummaryResult>.Ok(result));
    }

    /// <summary>
    /// Get top N best-selling products
    /// </summary>
    [HttpGet("products/top")]
    public async Task<IActionResult> GetTopProducts(
        [FromQuery] int topN = 10,
        CancellationToken cancellationToken = default)
    {
        if (topN <= 0 || topN > 100)
        {
            return BadRequest(ApiResponse<object>.Fail("topN must be between 1 and 100"));
        }

        var query = new GetTopProductsQuery(topN, null);
        var result = await _mediator.Send(query, cancellationToken);
        
        return Ok(ApiResponse<TopProductsResult>.Ok(result));
    }

    /// <summary>
    /// Get revenue time series with different granularities
    /// </summary>
    [HttpGet("revenue/timeseries")]
    public async Task<IActionResult> GetRevenueTimeSeries(
        [FromQuery] string period = "monthly",
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        CancellationToken cancellationToken = default)
    {
        if (!from.HasValue || !to.HasValue)
        {
            return BadRequest(ApiResponse<object>.Fail("Both 'from' and 'to' parameters are required"));
        }

        if (from.Value > to.Value)
        {
            return BadRequest(ApiResponse<object>.Fail("'from' date must be less than or equal to 'to' date"));
        }

        if (!Enum.TryParse<TimePeriod>(period, true, out var timePeriod))
        {
            return BadRequest(ApiResponse<object>.Fail("Invalid period. Valid values are: daily, weekly, monthly"));
        }

        var query = new GetRevenueTimeSeriesQuery(timePeriod, new DateRangeFilter(from.Value, to.Value));
        var result = await _mediator.Send(query, cancellationToken);
        
        return Ok(ApiResponse<RevenueTimeSeriesResult>.Ok(result));
    }

    /// <summary>
    /// Rebuild all materialized views from EventStore (Admin only)
    /// </summary>
    [HttpPost("admin/rebuild")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> RebuildMaterializedViews(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting materialized views rebuild requested by user {UserId}", 
            User.Identity?.Name ?? "Unknown");

        var command = new RebuildMaterializedViewsCommand();
        var result = await _mediator.Send(command, cancellationToken);
        
        _logger.LogInformation("Materialized views rebuild completed. Processed {ProcessedCount} events in {Duration}", 
            result.ProcessedCount, result.Duration);
        
        return Ok(ApiResponse<RebuildResult>.Ok(result));
    }
}