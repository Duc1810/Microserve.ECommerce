using Production.Application.Commons;
using Production.Application.Commons.Interfaces;

namespace Production.Application.Features.Queries.GetCategories;

public class GetCategoriesQueryHandler : IQueryHandler<GetCategoriesQuery, Result<GetCategoriesResult>>
{
    private readonly IProductRepository _repository;
    private readonly ICacheService _cacheService;
    private readonly ILogger<GetCategoriesQueryHandler> _logger;

    public GetCategoriesQueryHandler(
        IProductRepository repository,
        ICacheService cacheService,
        ILogger<GetCategoriesQueryHandler> logger)
    {
        _repository = repository;
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<Result<GetCategoriesResult>> Handle(GetCategoriesQuery query, CancellationToken cancellationToken)
    {
        try
        {
            var cacheKey = CacheKeys.Category.Categories;
            var categories = await _cacheService.GetOrCreateAsync(
                cacheKey,
                async () =>
                {
                    _logger.LogInformation("[{Handler}] cache_miss_get_categories_querying_database", nameof(Handle));

                    var stats = await _repository.GetCategoriesStatsAsync(cancellationToken);
                    var result = stats
                        .Select(s => new CategoryDto(
                            s.Category,
                            s.Count,
                            s.AvgPrice,
                            s.MinPrice,
                            s.MaxPrice))
                        .OrderBy(s => s.Category)
                        .ToList();

                    _logger.LogInformation("[{Handler}] get_categories_from_database total_categories={TotalCategories}", nameof(Handle), result.Count);
                    return result;
                },
                CacheKeys.Expiration.Categories,
                cancellationToken);

            _logger.LogInformation("[{Handler}] get_categories_success total_categories={TotalCategories}", nameof(Handle), categories.Count);

            return Result<GetCategoriesResult>.ResponseSuccess(new GetCategoriesResult(categories));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{Handler}] get_categories_error", nameof(Handle));
            return Result<GetCategoriesResult>.ResponseError(
                code: ErrorCodes.InternalError,
                message: ErrorMessages.InternalServerError,
                status: HttpStatusCode.InternalServerError);
        }
    }
}
