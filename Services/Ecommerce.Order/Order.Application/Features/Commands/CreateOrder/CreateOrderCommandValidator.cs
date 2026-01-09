
namespace Order.Application.Features.Commands.CreateOrder;

public class CreateOrderCommandValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderCommandValidator()
    {
        RuleFor(x => x.Order.OrderName)
            .NotEmpty().WithMessage("OrderName is required");

        RuleFor(x => x.Order.CustomerId)
            .NotEmpty().WithMessage("CustomerId is required");

        RuleFor(x => x.Order.ShippingAddress)
            .NotNull().WithMessage("ShippingAddress is required");

        RuleFor(x => x.Order.ShippingAddress.ZipCode)
            .NotEmpty().WithMessage("ZipCode is required")
            .Matches(@"^\d{5}$").WithMessage("ZipCode must be exactly 5 digits");

        RuleFor(x => x.Order.OrderItems)
            .NotEmpty().WithMessage("OrderItems cannot be empty");

        RuleForEach(x => x.Order.OrderItems).ChildRules(items =>
        {
            items.RuleFor(i => i.ProductId)
                .NotEmpty().WithMessage("ProductId is required");

            items.RuleFor(i => i.Quantity)
                .GreaterThan(0).WithMessage("Quantity must be greater than 0");

            items.RuleFor(i => i.Price)
                .GreaterThan(0).WithMessage("Price must be greater than 0");
        });
    }
}
