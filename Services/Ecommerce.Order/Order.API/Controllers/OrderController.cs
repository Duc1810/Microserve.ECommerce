using BuildingBlocks.Observability.ApiResponse;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Order.Application.Dtos;
using Order.Application.Features.Commands.CreateOrder;
using Order.Application.UserCases.GetOrder;


namespace Order.API.Controllers
{
    [ApiController]
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiVersion("1.0")]
    [Authorize(Policy = "ApiScope")]
    public class OrderController : ControllerBase
    {
        private readonly IMediator _mediator;

        public OrderController(IMediator mediator)
        {
            _mediator = mediator;
        }


        [HttpPost]
        public async Task<IActionResult> CreateOrder([FromBody] OrderDto request)
        {
            var result = await _mediator.Send(new CreateOrderCommand(request));
            return result.ToActionResult();
        }

        [HttpGet]
        [Authorize(Policy = "ApiScope")]
        public async Task<IActionResult> GetOrdersByCustomer(GetOrdersSearchParams request)
        {
            var result = await _mediator.Send(new GetOrdersQuery(request));
            return result.ToActionResult();
        }

    }
}
