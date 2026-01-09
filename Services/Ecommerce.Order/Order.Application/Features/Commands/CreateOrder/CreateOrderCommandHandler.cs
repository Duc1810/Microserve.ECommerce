
namespace Order.Application.Features.Commands.CreateOrder;


public class CreateOrderCommandHandler : ICommandHandler<CreateOrderCommand, Result<CreateOrderResult>>
{
    private readonly IUnitOfWork _unitOfWork;
    private IPublishEndpoint _publishEndpoint;
    private readonly ILogger<CreateOrderCommandHandler> _logger;
    private const string AppBaseUrl = "https://localhost:7180";

    public CreateOrderCommandHandler(IUnitOfWork unitOfWork, IPublishEndpoint publishEndpoint, ILogger<CreateOrderCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    public async Task<Result<CreateOrderResult>> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        try
        {

            var payload = request.Order.ShippingAddress;

            var customer = await _unitOfWork.GetRepository<Customer>().GetByPropertyAsync(o => o.Id == request.Order.CustomerId);

            await EnsureCustomerAsync(request.Order.CustomerId, payload.UserName, payload.EmailAddress, cancellationToken, true);

            var order = CreateNewOrder(request.Order);

            await _unitOfWork.GetRepository<OrderEntity>().AddAsync(order);

            await _unitOfWork.SaveAsync();

            await PublishOrderCreatedAsync(order, request.Order, cancellationToken);

            return Result<CreateOrderResult>.ResponseSuccess(new CreateOrderResult(order.Id), Messages.OrderCreatedSuccessfully);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CreateOrder] unexpected_error customerId={request.Order.CustomerId}", request.Order.CustomerId);
            return Result<CreateOrderResult>.ResponseError(
            code: ErrorCodes.InternalError,
            message: ErrorMessages.InternalServerError,
            status: HttpStatusCode.InternalServerError
            );
        }
    }



    private async Task EnsureCustomerAsync(Guid customerId, string userName, string email, CancellationToken ct, bool save = false)
    {
        var customerRepository = _unitOfWork.GetRepository<Customer>();

        var existing = await customerRepository.GetByPropertyAsync(c => c.Id == customerId);
        if (existing != null)
        {
            return;
        }

        var newCustomer = Customer.Create(
            id: customerId,
            name: userName,
            email: email
        );

        await customerRepository.AddAsync(newCustomer);

        if (save)
        {
            await _unitOfWork.SaveAsync();
        }
    }



    private static OrderEntity CreateNewOrder(OrderDto orderDto)
    {
        var shippingAddress = Address.Of(orderDto.ShippingAddress.UserName, orderDto.ShippingAddress.EmailAddress,
            orderDto.ShippingAddress.AddressLine, orderDto.ShippingAddress.State, orderDto.ShippingAddress.ZipCode);
        var billingAddress = Address.Of(orderDto.ShippingAddress.UserName, orderDto.ShippingAddress.EmailAddress,
            orderDto.ShippingAddress.AddressLine, orderDto.ShippingAddress.State, orderDto.ShippingAddress.ZipCode);

        var newOrder = OrderEntity.Create(
                id: Guid.NewGuid(),
                customerId: orderDto.CustomerId,
                orderName: orderDto.OrderName,
                shippingAddress: shippingAddress,
                billingAddress: billingAddress

                );

        foreach (var orderItemDto in orderDto.OrderItems)
        {
            newOrder.Add(orderItemDto.ProductId, orderItemDto.Quantity, orderItemDto.Price);
        }
        return newOrder;
    }


    private async Task PublishOrderCreatedAsync(
        OrderEntity order,
        OrderDto orderDto,
        CancellationToken cancellationToken)
    {
        var createOrderEvent = new CreatedEvent
        {
            UserId = orderDto.CustomerId,
            Title = Messages.OrderCreatedTitle,
            Message = Messages.FormatOrderCreatedMessage(order.Id, order.OrderName, order.OrderItems.Count, order.TotalPrice),
            Href = $"{AppBaseUrl}/Orders/{order.Id}",
            Email = orderDto.ShippingAddress.EmailAddress,
            FullName = orderDto.ShippingAddress.UserName,
            OrderId = order.Id,
            Price = order.TotalPrice,
            TotalItem = order.OrderItems.Count,
            Items = order.OrderItems.Select(oi => new CreatedEventItem
            {
                ProductId = oi.ProductId,
                Quantity = oi.Quantity
            }).ToList()
        };

        await _publishEndpoint.Publish(createOrderEvent, cancellationToken);
    }
}


