namespace Dashboard.Application.DTOs;

public record OrderItemDto(
    Guid ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice
);