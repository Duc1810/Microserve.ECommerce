
namespace Cart.Application.Features.Cart.Commands.CheckoutCart;

public sealed class CheckoutCartCommandValidator : AbstractValidator<CheckoutCartCommand>
{
    public CheckoutCartCommandValidator()
    {
        RuleFor(x => x.BasketCheckoutDto)
            .NotNull().WithMessage("BasketCheckoutDto can't be null");

        When(x => x.BasketCheckoutDto != null, () =>
        {
            RuleFor(x => x.BasketCheckoutDto.AddressLine)
                .NotEmpty().WithMessage("AddressLine is required");

            RuleFor(x => x.BasketCheckoutDto.State)
                .NotEmpty().WithMessage("State is required")
                .Matches(@"^\d{5}$").WithMessage("State must be exactly digits"); 

            RuleFor(x => x.BasketCheckoutDto.ZipCode)
                .NotEmpty().WithMessage("ZipCode is required")
                .Matches(@"^\d{5}$").WithMessage("ZipCode must be exactly 5 digits");
        });
    }
}

