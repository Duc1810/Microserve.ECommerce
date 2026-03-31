using BuildingBlocks.Observability.ApiResponse;
using MassTransit.Mediator;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Production.Application.Dtos.Products;
using Production.Application.Features.Queries.GetCategories;
using Production.Application.Features.Queries.GetCategoryStats;
using Production.Application.Features.Queries.GetProductCursor;

namespace Production.API.Controllers.V2
{
    [ApiController]
    [ApiVersion("2.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class ProductsController : ControllerBase
    {
        private readonly ISender _mediator;
        private readonly ILogger<ProductsController> _logger;

        public ProductsController(ISender mediator, ILogger<ProductsController> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        [HttpGet("cursor")]
        public async Task<IActionResult> GetByCursor(
        [FromQuery] ProductCursorParams param)
        {
            var result = await _mediator.Send(
                new GetProductsCursorQuery { Params = param });

            return result.ToActionResult();
        }

        [HttpGet("categories")]
        public async Task<IActionResult> GetCategories(CancellationToken cancellationToken)
        {
            _logger.LogInformation("[{Action}] get_categories_request_received", nameof(GetCategories));

            var result = await _mediator.Send(new GetCategoriesQuery(), cancellationToken);
            return result.ToActionResult();
        }

        [HttpGet("category-stats")]
        public async Task<IActionResult> GetCategoryStats(CancellationToken cancellationToken)
        {
            _logger.LogInformation("[{Action}] get_category_stats_request_received", nameof(GetCategoryStats));

            var result = await _mediator.Send(new GetCategoryStatsQuery(), cancellationToken);
            return result.ToActionResult();
        }
    }
}
