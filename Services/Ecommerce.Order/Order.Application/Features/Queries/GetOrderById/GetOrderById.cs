
using static Order.Application.Features.Queries.GetOrderById.GetOrderQuery;

namespace Order.Application.Features.Queries.GetOrderById;


public class GetOrderByIdHandler : IQueryHandler<GetOrderByIdQuery, Result<GetOrderByIdResult>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GetOrderByIdHandler> _logger;
    private readonly ICurrentUser _currentUser;

    public GetOrderByIdHandler(IUnitOfWork unitOfWork, ILogger<GetOrderByIdHandler> logger, ICurrentUser user)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _currentUser = user;
    }

    public async Task<Result<GetOrderByIdResult>> Handle(GetOrderByIdQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var orderRepository = _unitOfWork.GetRepository<DomainOrder>();

            var order = await orderRepository.GetByPropertyAsync(
                o => o.Id == request.OrderId,
                includeProperties: nameof(DomainOrder.OrderItems)
            );

            if (order == null)
            {
                _logger.LogWarning("[GetOrderById] not_found OrderId={OrderId}", request.OrderId);
                return Result<GetOrderByIdResult>.ResponseError(StatusCodeErrors.OrderNotFound, Messages.OrderNotFound, HttpStatusCode.NotFound);
            }
            return Result<GetOrderByIdResult>.ResponseSuccess(new GetOrderByIdResult(order));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"[CreateOrder] unexpected_error customerId={_currentUser.UserId}");
            return Result<GetOrderByIdResult>.ResponseError(
            code: ErrorCodes.InternalError,
            message: ErrorMessages.InternalServerError,
            status: HttpStatusCode.InternalServerError
            );
        }
    }
}
