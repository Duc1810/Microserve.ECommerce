

namespace Production.Application.Dtos
{
    public class ProductProfile : Profile
    {
        public ProductProfile()
        {
            CreateMap<Production.Domain.Entities.Product, ProductDto>();
        }
    }
}
