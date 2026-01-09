using Order.Domain.Enums;

namespace Order.Application.Dtos.Validators;

public class GetOrdersSearchParamsValidator : AbstractValidator<GetOrdersSearchParams>
{
    private static readonly HashSet<string> AllowedSortFields = new(StringComparer.OrdinalIgnoreCase)
    {
        "CreatedAt", "OrderName", "TotalPrice", "Status"
    };

    public GetOrdersSearchParamsValidator()
    {
        RuleFor(x => x.PageNumber)
            .GreaterThanOrEqualTo(1)
            .WithMessage("PageNumber must be >= 1.");

        RuleFor(x => x.PageSize)
            .GreaterThanOrEqualTo(1).WithMessage("PageSize must be >= 1.")
            .LessThanOrEqualTo(200).WithMessage("PageSize must be <= 200.");

        RuleFor(x => x)
            .Must(x => !x.From.HasValue || !x.To.HasValue || x.From <= x.To)
            .WithMessage("From must be less than or equal to To.");

        RuleFor(x => x.StatusFilter)
            .Must(s => string.IsNullOrWhiteSpace(s) || Enum.TryParse<OrderStatus>(s.Trim(), true, out _))
            .WithMessage($"StatusFilter is invalid. Allowed values: {string.Join(", ", Enum.GetNames(typeof(OrderStatus)))}");

        RuleFor(x => x.SortBy)
            .Must(s => string.IsNullOrWhiteSpace(s) || AllowedSortFields.Contains(s.Trim()))
            .WithMessage($"SortBy is invalid. Allowed: {string.Join(", ", AllowedSortFields)}");

        RuleFor(x => x.OrderNameFilter)
            .Must(s => s is null || s.Length <= 200)
            .WithMessage("OrderNameFilter must be <= 200 characters.");
    }
}
