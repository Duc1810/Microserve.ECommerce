using BuildingBlocks.Observability.ApiResponse;
using MassTransit.Mediator;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Production.Application.Dtos.Products;
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
    }
}
