using Cart.Application.Features.Commands.CreateCart;
namespace Cart.Application.Features.Cart.Commands.CreateCartItem;

public sealed class CreateCartItemCommandValidator : AbstractValidator<CreateCartItemCommand>
{
    public CreateCartItemCommandValidator()
    {
        RuleFor(x => x.PayLoad.ProductId).NotEmpty().WithMessage("ProductId is required");
        RuleFor(x => x.PayLoad.Quantity).GreaterThan(0).WithMessage("Quantity must be greater than 0");
    }
}

