namespace Cart.Application.Dtos;

public record ProductDto(
    Guid Id,
    string Name,
    string Description,
    decimal Price,
    int Quantity,
    IReadOnlyList<string> Categories,
    string ImageFile
);
