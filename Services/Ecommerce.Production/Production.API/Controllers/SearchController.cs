using BuildingBlocks.Observability.ApiResponse;
using MassTransit.Mediator;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Production.Application.Features.Queries.GetSearchProduct;
using Production.Application.Features.Queries.GetSugestproduct;
using Production.Application.Features.Queries.HyridSearchProduct;

namespace Production.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/products/[controller]")]
public class SearchController : ControllerBase
{
    private readonly ISender _sender;
    private readonly ILogger<SearchController> _logger;

    public SearchController(ISender mediator, ILogger<SearchController> logger)
    {
        _sender = mediator;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(Application.Features.Queries.GetSearchProduct.SearchResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Search(
        [FromQuery] string? q,
        [FromQuery] string? category,
        [FromQuery] decimal? minPrice,
        [FromQuery] decimal? maxPrice,
        [FromQuery] bool? inStock,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {

            var query = new SearchProductsQuery(q, category, minPrice, maxPrice, inStock, page, pageSize);
            var result = await _sender.Send(query, cancellationToken);
            return result.ToActionResult();
    }

    // <summary>
    // Get hyrid search 
    [HttpGet("hyrid-search")]
    [ProducesResponseType(typeof(Application.Features.Queries.GetSearchProduct.SearchResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> HyridSearch(
        [FromQuery] string keyword,
        CancellationToken cancellationToken = default)
    {

        var query = new HyridSearchProductsQuery(keyword);
        var result = await _sender.Send(query, cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Get autocomplete suggestions for product names
    /// </summary>
    [HttpGet("suggestions")]
    [ProducesResponseType(typeof(List<string>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<string>>> GetSuggestions(
        [FromQuery] string prefix,
        [FromQuery] int limit = 10,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = new GetSuggestionsQuery(prefix, limit);
            var suggestions = await _sender.Send(query, cancellationToken);

            return Ok(suggestions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting suggestions");
            return StatusCode(500, new { error = "An error occurred getting suggestions" });
        }
    }

    /// <summary>
    /// Get category statistics
    /// </summary>
    //[HttpGet("categories")]
    //[ProducesResponseType(typeof(Dictionary<string, long>), StatusCodes.Status200OK)]
    //public async Task<ActionResult<Dictionary<string, long>>> GetCategoryStats(
    //    CancellationToken cancellationToken = default)
    //{
    //    try
    //    {
    //        var query = new GetCategoryStatsQuery();
    //        var stats = await _sender.Send(query, cancellationToken);
    //        return Ok(stats);
    //    }
    //    catch (Exception ex)
    //    {
    //        _logger.LogError(ex, "Error getting category statistics");
    //        return StatusCode(500, new { error = "An error occurred getting category statistics" });
    //    }
    //}
}

