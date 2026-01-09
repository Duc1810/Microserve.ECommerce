

namespace Cart.Application.Dtos;
public record CartItemDto(Guid ProductId, string ProductName, string Color, decimal Price, int Quantity);


