

namespace Order.Application.Dtos
{
    public record OrderItemDto(Guid ProductId, int Quantity, decimal Price);
}
