
namespace Cart.Application.Features.Cart.Commands.CheckoutCart;

public record CheckoutCartCommand(CheckOutCartRequest BasketCheckoutDto): ICommand<Result<CheckoutCartResult>>;

public record CheckoutCartResult(bool IsSuccess, string? OrderId = null, string? Message = null);

