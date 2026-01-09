using Cart.Application.Features.Cart.Commands.RemoveCartItem;
namespace Cart.Application.Features.Commands.RemoveCartItem;

public class RemoveCartItemHandler : ICommandHandler<RemoveCartItemCommand, Result<RemoveCartItemResult>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<RemoveCartItemHandler> _logger;

    public RemoveCartItemHandler(IUnitOfWork unitOfWork, ICurrentUser currentUser, ILogger<RemoveCartItemHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<Result<RemoveCartItemResult>> Handle(RemoveCartItemCommand command, CancellationToken cancellationToken)
    {
        try
        {
            var logContext = new { Command = nameof(RemoveCartItemCommand), ProductId = command.ProductId };

            // Get username from token
            var userName = _currentUser.UserName?.Trim();
            if (string.IsNullOrWhiteSpace(userName))
            {
                _logger.LogWarning("[RemoveCart] unauthorized_no_username");
                return Result<RemoveCartItemResult>.ResponseError(
                    code: ErrorCodes.Unauthorized,
                    message: ErrorMessages.UnauthorizedAccess,
                    status: HttpStatusCode.Unauthorized
                );
            }

            var cartItemRepository = _unitOfWork.GetRepository<CartItem>();

            // get the cart item belonging to this user
            var cartItem = await cartItemRepository.GetByPropertyAsync(
                i => i.ProductId == command.ProductId
                     && i.Cart != null
                     && i.Cart.UserName == userName,
                tracked: false
            );

            if (cartItem == null)
            {
                _logger.LogWarning($"[{nameof(RemoveCartItemHandler)}] cart_item_not_found {logContext}");
                return Result<RemoveCartItemResult>.ResponseError(
                    code: StatusCodeErrors.CartItemNotFound,
                    message: Messages.CartItemNotFound,
                    status: HttpStatusCode.NotFound
                );
            }

            // Delete item and save
            await cartItemRepository.DeleteAsyncById(cartItem.Id);
            await _unitOfWork.SaveAsync();

            return Result<RemoveCartItemResult>.ResponseSuccess(new RemoveCartItemResult(userName, command.ProductId, true),Messages.RemoveCartItemSuccessfully);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"[CreateCart] unexpected_error userId={_currentUser.UserId}");
            return Result<RemoveCartItemResult>.ResponseError(
                code: ErrorCodes.InternalError,
                message: ErrorMessages.InternalServerError,
                status: HttpStatusCode.InternalServerError
            );
        }
    }
}
