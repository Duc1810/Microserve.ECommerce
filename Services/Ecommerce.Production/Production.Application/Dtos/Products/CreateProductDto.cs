

namespace Production.Application.Dtos.Products;
public record CreateProductDto(
    string Name,
    List<string> Category,
    string Description,
    string ImageFile,
    decimal Price
);

