
namespace Order.Application.Features.Commands.CreateOrder;

public record CreateOrderCommand(OrderDto Order) : ICommand<Result<CreateOrderResult>>;

public record CreateOrderResult(Guid Id);
