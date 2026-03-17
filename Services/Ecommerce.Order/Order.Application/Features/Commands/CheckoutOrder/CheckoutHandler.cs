using BuildingBlocks.Commands;
using Order.Application.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Order.Application.Features.Commands.CheckoutOrder
{
    public class CheckoutHandler : ICommandHandler<CheckoutCommand, Result<CheckoutResponse>>
    {
        private readonly ICartServiceClient _cartServiceClient;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly ILogger<CheckoutHandler> _logger;

        public CheckoutHandler(
            ICartServiceClient cartServiceClient,
            IUnitOfWork unitOfWork,
            IPublishEndpoint publishEndpoint,
            ILogger<CheckoutHandler> logger)
        {
            _cartServiceClient = cartServiceClient;
            _unitOfWork = unitOfWork;
            _publishEndpoint = publishEndpoint;
            _logger = logger;
        }

        public async Task<Result<CheckoutResponse>> Handle(CheckoutCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Processing checkout for customer: {CustomerId}", request.CustomerId);

            try
            {
                // Get the latest Cart from CartService (Source of Truth)
                var cartResult = await _cartServiceClient.GetCartByUserIdAsync(request.CustomerId, cancellationToken);
                if (!cartResult.IsSuccess)
                {
                    return Result<CheckoutResponse>.Failure((Error)cartResult.Error!);
                }
                var cart = cartResult.Value;
                if (cart == null || !cart.Items.Any())
                {
                    return Result<CheckoutResponse>.Failure("CART_EMPTY", "Cannot checkout an empty cart", HttpStatusCode.BadRequest);
                }

                // Ensure Customer data exists locally
                await EnsureCustomerSync(request.CustomerId, request.ShippingAddress);

                // Create Order Entity from Cart Data
                var order = CreateOrderEntity(request, cart);

                // Persist to Database
                await _unitOfWork.GetRepository<OrderEntity>().AddAsync(order);
                await _unitOfWork.SaveAsync();

                // Publish Event to Trigger SAGA (Stock & Payment)
                await PublishCheckoutEvent(order, request.ShippingAddress, cancellationToken);

                // Return OrderId (Saga will eventually provide the Payment URL)
                return Result<CheckoutResponse>.ResponseSuccess(new CheckoutResponse(order.Id, string.Empty));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Checkout failed for customer {CustomerId}", request.CustomerId);
                return Result<CheckoutResponse>.Failure("CHECKOUT_ERROR", "An error occurred during checkout", HttpStatusCode.InternalServerError);
            }
        }

        private OrderEntity CreateOrderEntity(CheckoutCommand request, CartResponse cart)
        {
            var address = Address.Of(request.ShippingAddress.UserName, request.ShippingAddress.EmailAddress,
                                    request.ShippingAddress.AddressLine, request.ShippingAddress.State, request.ShippingAddress.ZipCode);

            var order = OrderEntity.Create(Guid.NewGuid(), request.CustomerId, request.OrderName, address, address);

            foreach (var item in cart.Items)
            {
                order.Add(item.ProductId, item.Quantity, item.Price);
            }
            return order;
        }

        private async Task EnsureCustomerSync(Guid customerId, AddressDto address)
        {
            var genericRepository = _unitOfWork.GetRepository<Customer>();
            var existing = await genericRepository.GetByPropertyAsync(c => c.Id == customerId);
            if (existing == null)
            {
                var newCustomer = Customer.Create(customerId, address.UserName, address.EmailAddress);
                await genericRepository.AddAsync(newCustomer);
                await _unitOfWork.SaveAsync();
            }
        }
        private async Task PublishCheckoutEvent(
        OrderEntity order,
        AddressDto address,
        CancellationToken cancellationToken)
        {
            _logger.LogInformation("[Checkout] Publishing OrderSubmittedEvent for OrderId: {OrderId}", order.Id);

            await _publishEndpoint.Publish<IOrderSubmittedEvent>(new
            {
                OrderId = order.Id,
                UserId = order.CustomerId.ToString(),
                TotalAmount = order.TotalPrice,

                Items = order.OrderItems.Select(item => new BuildingBlocks.Commands.OrderItemDto
                {
                    ProductId = item.ProductId.ToString(),
                    Quantity = item.Quantity,
                    UnitPrice = item.Price
                }).ToList()

            }, cancellationToken);

            _logger.LogInformation("[Saga] Order Submitted Event published successfully for OrderId: {OrderId}", order.Id);
        }
    }
}
