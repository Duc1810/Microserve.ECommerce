namespace Cart.Application.Features.Queries.GetCart;

public record GetCartByUserQuery() : IQuery<Result<GetCartByUserResult>>;

public record GetCartByUserResult(string UserName, List<CartItemDto> Items, decimal TotalPrice);
