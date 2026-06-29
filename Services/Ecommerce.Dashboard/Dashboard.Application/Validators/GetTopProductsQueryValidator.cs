using Dashboard.Application.Queries;
using FluentValidation;

namespace Dashboard.Application.Validators;

public class GetTopProductsQueryValidator : AbstractValidator<GetTopProductsQuery>
{
    public GetTopProductsQueryValidator()
    {
        RuleFor(x => x.TopN)
            .GreaterThan(0)
            .LessThanOrEqualTo(100)
            .WithMessage("TopN must be between 1 and 100");

        RuleFor(x => x.Filter)
            .Must(filter => filter == null || filter.FromDate <= filter.ToDate)
            .WithMessage("FromDate must be less than or equal to ToDate");
    }
}