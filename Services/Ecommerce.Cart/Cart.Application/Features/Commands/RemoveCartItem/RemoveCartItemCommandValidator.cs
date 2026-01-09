

namespace Cart.Application.Features.Cart.Commands.RemoveCartItem;

public sealed class RemoveCartItemCommandValidator : AbstractValidator<RemoveCartItemCommand>
{
    public RemoveCartItemCommandValidator()
    {

        RuleFor(x => x.ProductId)
            .NotEmpty().WithMessage("ProductId is required");
    }
}
