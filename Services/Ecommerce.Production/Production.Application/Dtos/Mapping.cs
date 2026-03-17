

namespace Production.Application.Dtos
{
    public class ProductProfile : Profile
    {
        public ProductProfile()
        {
            CreateMap<Production.Domain.Entities.Product, ProductDto>();
            CreateMap<Production.Domain.Entities.ProductDocument, ProductDto>();
            CreateMap<ProductSearchResult, ProductSearchItemDto>()
            .ForMember(dest => dest.Product, opt => opt.MapFrom(src => src.Product));
        }
    }
}
