using Dashboard.Application.Commands;
using FluentValidation;

namespace Dashboard.Application.Validators;

public class UpdateRevenueOnPaymentCommandValidator : AbstractValidator<UpdateRevenueOnPaymentCommand>
{
    public UpdateRevenueOnPaymentCommandValidator()
    {
        RuleFor(x => x.OrderId)
            .NotEmpty()
            .WithMessage("OrderId is required");

        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .WithMessage("Amount must be greater than 0");

        RuleFor(x => x.Items)
            .NotEmpty()
            .WithMessage("Items cannot be empty");

        RuleForEach(x => x.Items)
            .ChildRules(item =>
            {
                item.RuleFor(i => i.ProductId)
                    .NotEmpty()
                    .WithMessage("ProductId is required");

                item.RuleFor(i => i.Quantity)
                    .GreaterThan(0)
                    .WithMessage("Quantity must be greater than 0");

                item.RuleFor(i => i.UnitPrice)
                    .GreaterThan(0)
                    .WithMessage("UnitPrice must be greater than 0");
            });
    }
}