using Product.Application.Commons;
using ProductEntity = Production.Domain.Entities.Product;

namespace Production.Application.Features.Queries.GetProductById;

public class GetProductByIdQueryHandler
    : IQueryHandler<GetProductByIdQuery, Result<GetProductsByIdResult>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GetProductByIdQueryHandler> _logger;

    public GetProductByIdQueryHandler(IUnitOfWork unitOfWork, ILogger<GetProductByIdQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<GetProductsByIdResult>> Handle(GetProductByIdQuery query, CancellationToken cancellationToken)
    {
        try
        {
            var repo = _unitOfWork.GetRepository<ProductEntity>();
            var product = await repo.GetByPropertyAsync(p => p.Id == query.Id);

            if (product is null)
            {
                _logger.LogWarning("[{Handler}.{Method}] not_found id={Id}", nameof(GetProductByIdQueryHandler), nameof(Handle), query.Id);
                return Result<GetProductsByIdResult>.ResponseError(StatusCodeErrors.ProductNotFound, ErrorMessages.ProductNotFound, HttpStatusCode.NotFound);
            }

            return Result<GetProductsByIdResult>.ResponseSuccess(new GetProductsByIdResult(product));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"[{nameof(Handle)}] unexpected_error");
            return Result<GetProductsByIdResult>.ResponseError(
                code: ErrorCodes.InternalError,
                message: ErrorMessages.InternalServerError,
                status: HttpStatusCode.InternalServerError
            );
        }
    }
}
