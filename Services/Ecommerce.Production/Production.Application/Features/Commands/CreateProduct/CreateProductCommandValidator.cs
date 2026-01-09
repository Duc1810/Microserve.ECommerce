
namespace Production.Application.Features.Commands.CreateProduct;
public sealed class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator()
    {
        RuleFor(x => x.Product.Name)
            .NotEmpty().WithMessage("Product name is required")
            .MaximumLength(100).WithMessage("Name must not exceed 100 characters");

        RuleFor(x => x.Product.Category)
            .NotNull().WithMessage("Category is required")
            .Must(c => c.Any()).WithMessage("At least one category is required");

        RuleFor(x => x.Product.Description)
            .NotEmpty().WithMessage("Description is required");

        RuleFor(x => x.Product.ImageFile)
            .NotEmpty().WithMessage("Image file is required");

        RuleFor(x => x.Product.Price)
            .GreaterThan(0).WithMessage("Price must be greater than 0");
    }
}

