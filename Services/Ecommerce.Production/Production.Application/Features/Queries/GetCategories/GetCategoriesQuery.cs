namespace Production.Application.Features.Queries.GetCategories;

public record GetCategoriesQuery : IQuery<Result<GetCategoriesResult>>;

public record GetCategoriesResult(IReadOnlyList<CategoryDto> Categories);
