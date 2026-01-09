
using Order.Application.Features.Commands.CreateOrder;
using Refit;
namespace Cart.Application.Abstractions;
public interface IOrderApi
{
    [Post("/api/v1/Order")]
    Task<ApiResponseNew<CreateOrderResult>> CreateOrderAsync([Body] OrderDto order, CancellationToken ct = default);
}

