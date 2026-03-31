using Production.Application.Commons;
using Production.Application.Commons.Interfaces;

namespace Production.Application.Features.Queries.GetCategoryStats;

public class GetCategoryStatsQueryHandler : IQueryHandler<GetCategoryStatsQuery, Result<GetCategoryStatsResult>>
{
    private readonly IProductRepository _repository;
    private readonly ICacheService _cacheService;
    private readonly IElasticsearchService _elasticsearchService;
    private readonly ILogger<GetCategoryStatsQueryHandler> _logger;

    public GetCategoryStatsQueryHandler(
        IProductRepository repository,
        ICacheService cacheService,
        IElasticsearchService elasticsearchService,
        ILogger<GetCategoryStatsQueryHandler> logger)
    {
        _repository = repository;
        _cacheService = cacheService;
        _elasticsearchService = elasticsearchService;
        _logger = logger;
    }

    public async Task<Result<GetCategoryStatsResult>> Handle(GetCategoryStatsQuery query, CancellationToken cancellationToken)
    {
        try
        {
            var cacheKey = CacheKeys.Category.CategoryStats;
            var categories = await _cacheService.GetOrCreateAsync(
                cacheKey,
                async () =>
                {
                    _logger.LogInformation("[{Handler}] cache_miss_get_category_stats_querying_sources", nameof(Handle));

                    var elasticCategories = await _elasticsearchService.GetCategoryAggregationsAsync(cancellationToken);
                    var dbStats = await _repository.GetCategoriesStatsAsync(cancellationToken);

                    var dbStatsByCategory = dbStats.ToDictionary(
                        s => s.Category,
                        s => new { s.AvgPrice, s.MinPrice, s.MaxPrice },
                        StringComparer.OrdinalIgnoreCase);

                    var result = elasticCategories
                        .Select(s =>
                        {
                            var hasStats = dbStatsByCategory.TryGetValue(s.Key, out var stats);

                            return new CategoryDto(
                                s.Key,
                                s.Value,
                                hasStats ? stats!.AvgPrice : 0,
                                hasStats ? stats!.MinPrice : 0,
                                hasStats ? stats!.MaxPrice : 0);
                        })
                        .OrderBy(s => s.Category)
                        .ToList();

                    _logger.LogInformation("[{Handler}] get_category_stats_from_sources total_categories={TotalCategories}", nameof(Handle), result.Count);
                    return result;
                },
                CacheKeys.Expiration.CategoryStats,
                cancellationToken);

            _logger.LogInformation("[{Handler}] get_category_stats_success total_categories={TotalCategories}", nameof(Handle), categories.Count);

            return Result<GetCategoryStatsResult>.ResponseSuccess(new GetCategoryStatsResult(categories));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{Handler}] get_category_stats_error", nameof(Handle));
            return Result<GetCategoryStatsResult>.ResponseError(
                code: ErrorCodes.InternalError,
                message: ErrorMessages.InternalServerError,
                status: HttpStatusCode.InternalServerError);
        }
    }
}
