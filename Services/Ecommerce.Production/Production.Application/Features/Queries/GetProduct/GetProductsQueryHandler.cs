using Product.Application.Commons;
using ProductEntity = Production.Domain.Entities.Product;

namespace Production.Application.Features.Queries.GetProduct
{
    public class GetProductsQueryHandler
        : IQueryHandler<GetProductsQuery, Result<GetProductsResult>>
    {
        private readonly IUnitOfWork _unitOfRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<GetProductsQueryHandler> _logger;

        public GetProductsQueryHandler(IUnitOfWork unitOfWork, IMapper mapper, ILogger<GetProductsQueryHandler> logger)
        {
            _unitOfRepository = unitOfWork;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<Result<GetProductsResult>> Handle(GetProductsQuery query, CancellationToken cancellationToken)
        {
            var parameters = query.Params;

            var logContext = new { parameters.PageNumber, parameters.PageSize, query.Params.SortBy, query.Params.Descending, query.Params.NameFilter };

            try
            {
               
                // build filter and order by expressions
                var filter = FilterByName(query.Params.NameFilter);
                var orderBy = SortOrderBy(query.Params.SortBy);

                // query repository
                var productRepository = _unitOfRepository.GetRepository<ProductEntity>();

                // get paginated result with total count
                (IEnumerable<ProductEntity> items, long totalCount) = await productRepository.GetAllByPropertyWithCountAsync(
                    pageNumber: parameters.PageNumber,
                    pageSize: parameters.PageSize,
                    filter: filter,
                    orderBy: orderBy,
                    ascending: !query.Params.Descending
                );

                if (items is null || totalCount == 0)
                {
                    _logger.LogWarning($"[nameof(Handle)] not_found {logContext}");

                    return Result<GetProductsResult>.ResponseError(StatusCodeErrors.ProductNotFound, ErrorMessages.ProductNotFound, HttpStatusCode.NotFound);
                }

                // map to dto and wrap in paginated result
                var dtoList = _mapper.Map<List<ProductDto>>(items);

                var page = new PaginatedResult<ProductDto>(
                    pageIndex: parameters.PageNumber,
                    pageSize: parameters.PageSize,
                    count: totalCount,
                    data: dtoList
                );

                return Result<GetProductsResult>.ResponseSuccess(new GetProductsResult(page));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[{nameof(Handle)}] unexpected_error");
                return Result<GetProductsResult>.ResponseError(
                    code: ErrorCodes.InternalError,
                    message: ErrorMessages.InternalServerError,
                    status: HttpStatusCode.InternalServerError
                );
            }
        }


        private static Expression<Func<ProductEntity, bool>> FilterByName(string? nameFilter)
        {
            Expression<Func<ProductEntity, bool>> predicate = product => !product.IsDeleted && product.Quantity > 0;

            if (!string.IsNullOrWhiteSpace(nameFilter))
            {
                predicate = And(predicate, product => product.Name.Contains(nameFilter.Trim()));
            }
            return predicate;

            static Expression<Func<T, bool>> And<T>(Expression<Func<T, bool>> left, Expression<Func<T, bool>> right)
            {
                var p = Expression.Parameter(typeof(T), "p");
                var leftBody = new ReplaceParamVisitor(left.Parameters[0], p).Visit(left.Body)!;
                var rightBody = new ReplaceParamVisitor(right.Parameters[0], p).Visit(right.Body)!;
                return Expression.Lambda<Func<T, bool>>(Expression.AndAlso(leftBody, rightBody), p);
            }
        }

        private static Expression<Func<ProductEntity, object>>? SortOrderBy(string? sortBy)
        {
            if (string.IsNullOrWhiteSpace(sortBy)) return null;

            return sortBy.Trim() switch
            {
                nameof(ProductEntity.Name) => product => product.Name,
                nameof(ProductEntity.Price) => product => product.Price,
                nameof(ProductEntity.Quantity) => product => product.Quantity,
                nameof(ProductEntity.CreatedAt) => product => product.CreatedAt!,
                _ => null
            };
        }

        private class ReplaceParamVisitor : ExpressionVisitor
        {
            private readonly ParameterExpression _from, _to;
            public ReplaceParamVisitor(ParameterExpression from, ParameterExpression to)
            { _from = from; _to = to; }

            protected override Expression VisitParameter(ParameterExpression node)
                => node == _from ? _to : base.VisitParameter(node);
        }
    }
}
