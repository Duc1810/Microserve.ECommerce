namespace Production.Application.Dtos.Products;

public record CategoryDto(
    string Category,
    long Count,
    decimal AvgPrice,
    decimal MinPrice,
    decimal MaxPrice);
