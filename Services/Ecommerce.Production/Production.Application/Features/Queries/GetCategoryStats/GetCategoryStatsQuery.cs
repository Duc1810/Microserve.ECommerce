namespace Production.Application.Features.Queries.GetCategoryStats;

public record GetCategoryStatsQuery : IQuery<Result<GetCategoryStatsResult>>;

public record GetCategoryStatsResult(IReadOnlyList<CategoryDto> Categories);
