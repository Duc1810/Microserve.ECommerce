

namespace Order.Application.UserCases.GetOrder;
public class GetOrdersQueryHandler : IQueryHandler<GetOrdersQuery, Result<GetOrdersResult>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IMapper _mapper;
    private readonly ILogger<GetOrdersQueryHandler> _logger;

    public GetOrdersQueryHandler(
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IMapper mapper,
        ILogger<GetOrdersQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<Result<GetOrdersResult>> Handle(GetOrdersQuery request, CancellationToken cancellationToken)
    {
        try
        {

            var parameters = request.Params;

            var userId = _currentUser.UserId;
            if (string.IsNullOrWhiteSpace(userId))
            {
                _logger.LogWarning("[GetOrders] unauthorized_no_user");
                return Result<GetOrdersResult>.ResponseError(ErrorCodes.Unauthorized, ErrorMessages.UnauthorizedAccess, HttpStatusCode.Unauthorized);
            }

            var filter = BuildFilter(parameters, userId);
            var include = nameof(DomainOrder.OrderItems);
            var repo = _unitOfWork.GetRepository<DomainOrder>();

            var (orders, totalCount) = await repo.GetAllByPropertyWithCountAsync(
                filter: filter,
                includeProperties: include,
                pageNumber: parameters.PageNumber,
                pageSize: parameters.PageSize,
                ascending: parameters.SortAscending
            );

            if (orders == null || orders.Count == 0 || totalCount == 0)
            {
                _logger.LogWarning("[GetOrders] not_found userId={UserId}", userId);
                return Result<GetOrdersResult>.ResponseError(StatusCodeErrors.OrderNotFound, Messages.OrderNotFound, HttpStatusCode.NotFound);
            }

            var ordersDto = _mapper.Map<List<OrderDto>>(orders) ?? new List<OrderDto>();
            var paginated = new PaginatedResult<OrderDto>(
                pageIndex: parameters.PageNumber,
                pageSize: parameters.PageSize,
                count: totalCount,
                data: ordersDto
            );

            var result = new GetOrdersResult(paginated);
            return Result<GetOrdersResult>.ResponseSuccess(result, Messages.OrderFetchedSuccessfully);

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[GetOrders] unexpected_error customerId={request.Order.CustomerId}", _currentUser.UserId);
            return Result<GetOrdersResult>.ResponseError(
            code: ErrorCodes.InternalError,
            message: ErrorMessages.InternalServerError,
            status: HttpStatusCode.InternalServerError
            );
        }
    }

    private static Expression<Func<DomainOrder, bool>>? BuildFilter(GetOrdersSearchParams param, string userId)
    {
        Expression<Func<DomainOrder, bool>>? filter = null;

        void And(Expression<Func<DomainOrder, bool>> expr)
        {
            if (filter == null) filter = expr;
            else filter = CombineAnd(filter, expr);
        }

        var customerId = Guid.Parse(userId);
        And(o => o.CustomerId == customerId);

        if (!string.IsNullOrWhiteSpace(param.OrderNameFilter))
        {
            var name = param.OrderNameFilter.Trim();
            And(o => o.OrderName != null && o.OrderName.Contains(name));
        }

        if (!string.IsNullOrWhiteSpace(param.StatusFilter) &&
            Enum.TryParse<Domain.Enums.OrderStatus>(param.StatusFilter.Trim(), true, out var statusEnum))
        {
            And(o => o.Status == statusEnum);
        }

        if (param.From.HasValue)
            And(o => o.CreatedAt >= param.From.Value);

        if (param.To.HasValue)
            And(o => o.CreatedAt <= param.To.Value);

        return filter;
    }

    private static Expression<Func<T, bool>> CombineAnd<T>(
        Expression<Func<T, bool>> left,
        Expression<Func<T, bool>> right)
    {
        var param = left.Parameters[0];
        var replacedRightBody = new ParameterReplaceVisitor(right.Parameters[0], param).Visit(right.Body)!;
        var body = Expression.AndAlso(left.Body, replacedRightBody);
        return Expression.Lambda<Func<T, bool>>(body, param);
    }

    private sealed class ParameterReplaceVisitor : ExpressionVisitor
    {
        private readonly ParameterExpression _from;
        private readonly ParameterExpression _to;

        public ParameterReplaceVisitor(ParameterExpression from, ParameterExpression to)
        {
            _from = from;
            _to = to;
        }

        protected override Expression VisitParameter(ParameterExpression node)
            => node == _from ? _to : base.VisitParameter(node);
    }
}
