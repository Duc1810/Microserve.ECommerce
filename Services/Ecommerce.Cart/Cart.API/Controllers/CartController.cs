using BuildingBlocks.Observability.ApiResponse;
using Cart.Application.Abtractions.Dtos;
using Cart.Application.Dtos;
using Cart.Application.Features.Cart.Commands.CheckoutCart;
using Cart.Application.Features.Cart.Commands.RemoveCartItem;
using Cart.Application.Features.Commands.CreateCart;
using Cart.Application.Features.Queries.GetCart;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cart.API.Controllers
{
    [ApiController]
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiVersion("1.0")]
    [Authorize(Policy = "ApiScope")]
    public class CartController : ControllerBase
    {
        private readonly IMediator _mediator;


        public CartController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost]
        public async Task<IActionResult> CreateCartItem([FromBody] CreateCartItemRequest request)
        {

            var result = await _mediator.Send(new CreateCartItemCommand(request));
            return result.ToCreatedAtActionResult(nameof(GetCartByUser), new { id = result.Value! });
        }

        [HttpGet]
        public async Task<IActionResult> GetCartByUser()
        {
            var result = await _mediator.Send(new GetCartByUserQuery());
            return result.ToActionResult();
        }

        [HttpPost("checkout")]
        public async Task<IActionResult> CheckoutCart([FromBody] CheckOutCartRequest request)
        {
            var result = await _mediator.Send(new CheckoutCartCommand(request));
            return result.ToActionResult();
        }

        [HttpDelete("{productId:guid}")]
        public async Task<IActionResult> RemoveCartItem(Guid productId, CancellationToken ct)
        {
            var result = await _mediator.Send(new RemoveCartItemCommand(productId), ct);
            return result.ToActionResult();
        }

    }
}
