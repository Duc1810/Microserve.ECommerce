using BuildingBlocks.Results;

namespace Cart.Application.Features.Commands.CreateCart;
public record CreateCartItemCommand(CreateCartItemRequest PayLoad) : ICommand<Result<CreateCartItemResult>>;

public record CreateCartItemResult(string UserName, Guid ProductId);

