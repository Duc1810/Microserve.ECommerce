using Cart.Application.Features.Cart.Commands.CheckoutCart;
using Order.Application.Features.Commands.CreateOrder;
namespace Cart.Application.Features.Commands.CheckoutCart;

public class CheckoutCartCommandHandler
    : ICommandHandler<CheckoutCartCommand, Result<CheckoutCartResult>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOrderApi _orderApi;
    private readonly ICurrentUser _currentUser;
    private readonly IRedisService _redisService;
    private readonly ILogger<CheckoutCartCommandHandler> _logger;

    public CheckoutCartCommandHandler(
        IUnitOfWork unitOfWork,
        IOrderApi orderApi,
        ICurrentUser currentUser,
        IRedisService redisService,
        ILogger<CheckoutCartCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _orderApi = orderApi;
        _currentUser = currentUser;
        _redisService = redisService;
        _logger = logger;
    }

    public async Task<Result<CheckoutCartResult>> Handle(CheckoutCartCommand command, CancellationToken cancellationToken)
    {
        try
        {
            // Get user info from token
            var username = _currentUser.UserName?.Trim();
            var email = _currentUser.Email?.Trim();
            var userId = _currentUser.UserId;

            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(username))
            {
                _logger.LogWarning("[Checkout] unauthorized_no_username");
                return Result<CheckoutCartResult>.ResponseError(
                    code: ErrorCodes.Unauthorized,
                    message: ErrorMessages.UnauthorizedAccess,
                    status: HttpStatusCode.Unauthorized
                );
            }

            // Get cart by user name
            var cart = await GetCartByUsernameAsync(username, cancellationToken);
            if (cart is null || cart.CartItems is null || cart.CartItems.Count == 0)
            {
                var reason = cart is null ? Messages.CartNotFoundForUser : Messages.CartItemsEmpty;
                _logger.LogWarning("[CheckoutCart] cart_invalid username={User} reason={Reason}", username, reason);
                return Result<CheckoutCartResult>.ResponseError(
                    code: StatusCodeErrors.CartNotFound,
                    message: reason,
                    status: HttpStatusCode.NotFound
                );
            }

            // Map to order
            var orderItems = MapToOrderItems(cart);
            var shipping = MapToShippingAddress(username, email, command.BasketCheckoutDto);
            var orderName = CreateOrderName(username);

            // Prepare order dto
            var order = new OrderDto(
                CustomerId: new Guid(userId),
                OrderName: orderName,
                ShippingAddress: shipping,
                OrderItems: orderItems
            );

            // Create order by Order API
            var createOrderResult = await CreateOrderAsync(order, cancellationToken);
            if(!createOrderResult.IsSuccess)
        {
                var err = createOrderResult.Error!.Value;
                // Không clear cart khi tạo order thất bại
                return Result<CheckoutCartResult>.ResponseError(
                    code: err.Code,
                    message: err.Message,
                    status: err.StatusCode,
                    details: err.Details
                );
            }

            // Clear cart
            await ClearCartAsync(username, cart.Id, cancellationToken);

            return Result<CheckoutCartResult>.ResponseSuccess(new CheckoutCartResult(true, createOrderResult.ToString()), Messages.OrderCreatedSuccessfully);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"[CreateCart] unexpected_error userId={_currentUser.UserId}");
            return Result<CheckoutCartResult>.ResponseError(
                code: ErrorCodes.InternalError,
                message: ErrorMessages.InternalServerError,
                status: HttpStatusCode.InternalServerError
            );
        }
    }

    private async Task<ShoppingCart?> GetCartByUsernameAsync(string userName, CancellationToken cancellationToken)
    {
        var cartRepo = _unitOfWork.GetRepository<ShoppingCart>();

        var include = nameof(ShoppingCart.CartItems);
        return await cartRepo.GetByPropertyAsync(c => c.UserName == userName, true, includeProperties: include);
    }

    private static List<OrderItemDto> MapToOrderItems(ShoppingCart cart)
    {

        return cart.CartItems!
          .Select(i => new OrderItemDto(i.ProductId, i.Quantity, i.Price))
          .ToList();
    }
    private static AddressDto MapToShippingAddress(string userName, string? email, CheckOutCartRequest dto)
    {

        return new AddressDto(userName, email ?? string.Empty, dto.AddressLine, dto.State, dto.ZipCode);
    }
    private static string CreateOrderName(string userName)
    {

        return $"{userName}-{DateTime.UtcNow.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture)}";
    }

    private async Task<Result<Guid>> CreateOrderAsync(OrderDto orderDto, CancellationToken cancellationToken)
    {

        var response = await _orderApi.CreateOrderAsync(orderDto, cancellationToken);

        if (!response.Success || response.Data is null)
        {
            var errors = response.Errors ?? "No response content";

            _logger.LogError($"[CreateOrder] order_api_failed error={errors} with {orderDto.CustomerId}");

            return Result<Guid>.ResponseError(
                code: StatusCodeErrors.Order_APi_Failed,
                message: Messages.OrderApiFailed(response.Errors),
                status: System.Net.HttpStatusCode.BadRequest,
                details: response.Errors
            );
        }
        return Result<Guid>.ResponseSuccess(response.Data.Id, "Order created successfully");
    }


    private async Task ClearCartAsync(string userName, Guid cartId, CancellationToken cancellationToken)
    {
        var cartRepo = _unitOfWork.GetRepository<ShoppingCart>();
        await cartRepo.DeleteAsyncById(cartId);
        await _unitOfWork.SaveAsync();
        await _redisService.RemoveAsync($"cart:{userName}");
    }
}

