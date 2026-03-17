namespace Production.Application.Features.Queries.HyridSearchProduct;

public record HyridSearchProductsQuery(
    string Keyword
) : IQuery<Result<SearchResponseDto>>;

public class SearchResponseDto
{
    public List<ProductSearchItemDto> Products { get; set; } = new();
    public long TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public string? Query { get; set; }
    public SearchFiltersDto Filters { get; set; } = new();
}

public class SearchFiltersDto
{
    public string? Category { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public bool? InStockOnly { get; set; }
}
