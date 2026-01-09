
using System.ComponentModel;

namespace Production.Application.Dtos.Products;

public class GetProductsSearchParams
{
    [DefaultValue(1)]
    public int PageNumber { get; init; } = 1;

    [DefaultValue(10)]
    public int PageSize { get; init; } = 10;

    public string? NameFilter { get; init; }
    public string? SortBy { get; init; }

    [DefaultValue(true)]
    public bool Descending { get; init; } = true;
}

