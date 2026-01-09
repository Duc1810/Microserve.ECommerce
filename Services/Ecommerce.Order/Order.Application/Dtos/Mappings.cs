using AutoMapper;
using Order.Domain.ValueObjects;
namespace Order.Application.Dtos
{
    public class OrderMappingProfile : Profile
    {
        public OrderMappingProfile()
        {
            CreateMap<Address, AddressDto>();
            CreateMap<Order.Domain.Models.OrderItem, OrderItemDto>();
            CreateMap<Order.Domain.Models.Order, OrderDto>()
           .ForCtorParam("CustomerId", opt => opt.MapFrom(s => s.CustomerId))
           .ForCtorParam("OrderName", opt => opt.MapFrom(s => s.OrderName))
           .ForCtorParam("ShippingAddress", opt => opt.MapFrom(s => s.ShippingAddress))
           .ForCtorParam("OrderItems", opt => opt.MapFrom(s => s.OrderItems));
        }
    }
}
