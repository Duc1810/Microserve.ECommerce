using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Order.Application.Dtos;

public class GetOrdersSearchParams
{
    public int PageNumber { get; init; } = 1;

    public int PageSize { get; init; } = 10;

    public string? OrderNameFilter { get; init; }

    public string? StatusFilter { get; init; }

    public DateTime? From { get; init; }

    public DateTime? To { get; init; }

    public string? SortBy { get; init; }
    public bool SortAscending { get; init; } = true;
}
