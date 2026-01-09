
namespace Production.Application.Features.Commands.UpdateProduct;

public record UpdateProductCommand(
    Guid Id,
    string Name,
    List<string> Category,
    string Description,
    string ImageFile,
    decimal Price,
    int Quantity
) : ICommand<Result<UpdateProductResult>>, IVersionBumpRequest
{
    public IEnumerable<string> VersionScopesToBump()
    {
        yield return "product:list:ver";
        foreach (var c in Category ?? Enumerable.Empty<string>())
            yield return $"product:list:ver:category:{c}";
    }
}

public record UpdateProductResult(Guid Id);
