
namespace Production.Application.Features.Queries.GetProduct;

public class GetProductsQueryValidator : AbstractValidator<GetProductsQuery>
{
    public GetProductsQueryValidator()
    {
        RuleFor(x => x.Params).NotNull().WithMessage("Params is required.");

        When(x => x.Params != null, () =>
        {
            RuleFor(x => x.Params!.PageNumber).GreaterThanOrEqualTo(1);
            RuleFor(x => x.Params!.PageSize).InclusiveBetween(1, 200);
            RuleFor(x => x.Params!.SortBy).MaximumLength(50);
            RuleFor(x => x.Params!.NameFilter).MaximumLength(200);
        });
    }
}

