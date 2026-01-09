namespace Cart.Application.Features.Queries.GetCart;
public class GetCartByUserHandler : IQueryHandler<GetCartByUserQuery, Result<GetCartByUserResult>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GetCartByUserHandler> _logger;
    private readonly ICurrentUser _currentUser;

    public GetCartByUserHandler(IUnitOfWork unitOfWork, ILogger<GetCartByUserHandler> logger, ICurrentUser currentUser)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _currentUser = currentUser;
    }

    public async Task<Result<GetCartByUserResult>> Handle(GetCartByUserQuery query, CancellationToken cancellationToken)
    {
        try
        {
            var userName = _currentUser.UserName?.Trim();
            if (string.IsNullOrWhiteSpace(userName))
            {
                _logger.LogWarning("[GetCartByUser] unauthorized_no_username");
                return Result<GetCartByUserResult>.ResponseError(
                    code: ErrorCodes.Unauthorized,
                    message: ErrorMessages.UnauthorizedAccess,
                    status: HttpStatusCode.Unauthorized
                );
            }

            var logContext = new { Command = nameof(GetCartByUserQuery), userName };

            var cartRepo = _unitOfWork.GetRepository<ShoppingCart>();
            var cart = await cartRepo.GetByPropertyAsync(
                c => c.UserName == userName,
                includeProperties: nameof(ShoppingCart.CartItems)
            );

            if (cart == null)
            {
                _logger.LogWarning("[{Handler}] cart_not_found {LogContext}", nameof(GetCartByUserHandler), logContext);
                return Result<GetCartByUserResult>.ResponseError(
                    code: StatusCodeErrors.CartNotFound,
                    message: Messages.CartNotFound,
                    status: HttpStatusCode.NotFound
                );
            }

            if (cart.CartItems == null || cart.CartItems.Count == 0)
            {
                _logger.LogInformation($"[{nameof(GetCartByUserHandler)}] cart_empty {logContext}");
                return Result<GetCartByUserResult>.ResponseSuccess(new GetCartByUserResult(cart.UserName, new List<CartItemDto>(), 0m), Messages.CartEmpty
                );
            }

            var items = cart.CartItems
                .Select(i => new CartItemDto(i.ProductId, i.ProductName, i.Color!, i.Price, i.Quantity))
                .ToList();

            var result = new GetCartByUserResult(cart.UserName, items, cart.TotalPrice);
            return Result<GetCartByUserResult>.ResponseSuccess(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"[RemoveCart] unexpected_error userId={_currentUser.UserId}");
            return Result<GetCartByUserResult>.ResponseError(
                code: ErrorCodes.InternalError,
                message: ErrorMessages.InternalServerError,
                status: HttpStatusCode.InternalServerError
            );
        }
    }
}
