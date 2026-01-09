

namespace Production.Application.Features.Commands.CreateProduct;
public record CreateProductCommand(CreateProductDto  Product) : ICommand<Result<CreateProductResult>>, IVersionBumpRequest
{
    public IEnumerable<string> VersionScopesToBump()
    {
        yield return "product:list:ver";

        foreach (var c in Product.Category ?? Enumerable.Empty<string>())
            yield return $"product:list:ver:category:{c}";
    }
}

public record CreateProductResult(Guid Id);

