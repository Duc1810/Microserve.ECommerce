using BuildingBlocks.Results;

namespace Cart.Application.Features.Cart.Commands.RemoveCartItem;

public record RemoveCartItemCommand(Guid ProductId) : ICommand<Result<RemoveCartItemResult>>;

public record RemoveCartItemResult(string UserName, Guid ProductId, bool IsRemoved);

