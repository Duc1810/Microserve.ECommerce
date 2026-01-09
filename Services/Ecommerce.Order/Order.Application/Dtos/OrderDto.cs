

using Order.Domain.Enums;

namespace Order.Application.Dtos
{
    public record OrderDto(
    Guid CustomerId,
    string OrderName,
    AddressDto ShippingAddress,
    List<OrderItemDto> OrderItems);
}
