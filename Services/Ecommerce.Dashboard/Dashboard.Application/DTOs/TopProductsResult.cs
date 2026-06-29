namespace Dashboard.Application.DTOs;

public record TopProductsResult(List<TopProductItem> Items);

public record TopProductItem(
    Guid ProductId,
    string ProductName,
    int TotalQuantitySold,
    decimal TotalRevenue
);