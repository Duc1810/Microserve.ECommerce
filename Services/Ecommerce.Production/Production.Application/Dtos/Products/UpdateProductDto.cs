namespace Production.Application.Dtos.Products;

public record UpdateProductDto(
    string Name,
    List<string> Category,
    string Description,
    string ImageFile,
    decimal Price,
    int Quantity
);
