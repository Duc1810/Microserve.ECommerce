using BuildingBlocks.Observability.ApiResponse;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Production.Application.Dtos.Products;
using Production.Application.Features.Commands.CreateProduct;
using Production.Application.Features.Commands.UpdateProduct;
using Production.Application.Features.Queries.GetProduct;
using Production.Application.Features.Queries.GetProductById;

namespace Production.API.Controllers
{
    [ApiController]
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiVersion("1.0")]

    public class ProductController : ControllerBase
    {
        private readonly ISender _sender;

        public ProductController(ISender sender)
        {
            _sender = sender;
        }

        [HttpGet]
        public async Task<IActionResult> GetProducts([FromQuery] GetProductsSearchParams query)
        {
            var result = await _sender.Send(new GetProductsQuery(query));
            return result.ToActionResult();
        }

        [HttpGet("{id}")]

        public async Task<IActionResult> GetProductById([FromRoute] Guid id)
        {
            var result = await _sender.Send(new GetProductByIdQuery(id));
            return result.ToActionResult();
        }

        [HttpPost]
        [Authorize(Policy = "ApiScope")]
        public async Task<IActionResult> Create([FromBody] CreateProductDto request)
        {
            var result = await _sender.Send(new CreateProductCommand(request));
            return result.ToCreatedAtActionResult(nameof(GetProductById), new { id = result.Value! });
        }

        [HttpPut]
        [Authorize(Policy = "ApiScope")]
        public async Task<IActionResult> Update([FromBody] UpdateProductCommand command)
        {
            var result = await _sender.Send(command);
            return result.ToActionResult();
        }
    }
}
