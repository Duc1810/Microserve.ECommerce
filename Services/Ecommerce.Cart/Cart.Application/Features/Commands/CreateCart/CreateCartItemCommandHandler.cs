
namespace Cart.Application.Features.Commands.CreateCart;

public class CreateCartItemCommandHandler : ICommandHandler<CreateCartItemCommand, Result<CreateCartItemResult>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IProductService _productService;
    private readonly ILogger<CreateCartItemCommandHandler> _logger;
    private const string DefaultColor = "Red";

    public CreateCartItemCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IProductService productService,
        ILogger<CreateCartItemCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _productService = productService;
        _logger = logger;
    }

    public async Task<Result<CreateCartItemResult>> Handle(CreateCartItemCommand command, CancellationToken cancellationToken)
    {
        try
        {
            var payload = command.PayLoad;

            var userName = _currentUser.UserName?.Trim();
            if (string.IsNullOrWhiteSpace(userName))
            {
                _logger.LogWarning("[CreateCart] unauthorized_no_username");
                return Result<CreateCartItemResult>.ResponseError(
                    code: ErrorCodes.Unauthorized,
                    message: ErrorMessages.UnauthorizedAccess,
                    status: HttpStatusCode.Unauthorized
                );
            }


            var product = await _productService.GetProductAsync(payload.ProductId);
            if (product is null)
            {
                _logger.LogWarning($"[CreateCart] product_not_found ProductId={payload.ProductId}");
                return Result<CreateCartItemResult>.ResponseError(
                    code: StatusCodeErrors.CartNotFound,
                    message: Messages.ProductNotFound(payload.ProductId),
                    status: HttpStatusCode.NotFound
                );
            }


            var cart = await GetOrCreateCartAsync(userName, cancellationToken);

            await AddOrUpdateCartItemAsync(cart, payload.Quantity, product, cancellationToken);

            await _unitOfWork.SaveAsync();

            return Result<CreateCartItemResult>.ResponseSuccess(new CreateCartItemResult(userName, payload.ProductId), Messages.CreateCartItemSuccessfully);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"[CreateCart] unexpected_error userId={_currentUser.UserId}");
            return Result<CreateCartItemResult>.ResponseError(
                code: ErrorCodes.InternalError,
                message: ErrorMessages.InternalServerError,
                status: HttpStatusCode.InternalServerError
            );
        }
    }


    private async Task<ShoppingCart> GetOrCreateCartAsync(string userName, CancellationToken cancellationToken)
    {
        var cartRepository = _unitOfWork.GetRepository<ShoppingCart>();
        var cart = await cartRepository.GetByPropertyAsync(c => c.UserName == userName);
        if (cart != null)
        {
            return cart;
        }

        cart = new ShoppingCart { UserName = userName, CartItems = new List<CartItem>() };
        await cartRepository.AddAsync(cart);
        return cart;
    }

    private async Task AddOrUpdateCartItemAsync(ShoppingCart cart, int quantity, ProductDto product, CancellationToken cancellationToken)
    {
        var cartItemRepository = _unitOfWork.GetRepository<CartItem>();

        var cartItem = await cartItemRepository.GetByPropertyAsync(i => i.CartId == cart.Id && i.ProductId == product.Id);
        if (cartItem != null)
        {
            cartItem.Quantity += quantity;
            cartItem.Price = product.Price;
            await cartItemRepository.UpdateAsync(cartItem);
            return;
        }

        cartItem = new CartItem
        {
            CartId = cart.Id,
            ProductId = product.Id,
            ProductName = product.Name,
            Color = DefaultColor,
            Price = product.Price,
            Quantity = quantity
        };

        await cartItemRepository.AddAsync(cartItem);
    }
}
