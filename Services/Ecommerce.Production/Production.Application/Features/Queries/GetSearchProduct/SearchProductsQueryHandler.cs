using MediatR;
using Production.Application.Commons.Interfaces;
using Production.Application.Dtos.Products;
using Production.Application.Features.Queries.GetProduct;
using Production.Application.Features.Queries.GetProductById;
using Production.Domain.Entities;

namespace Production.Application.Features.Queries.GetSearchProduct;
public class SearchProductsQueryHandler : IQueryHandler<SearchProductsQuery, Result<SearchResponseDto>>
{
    private readonly IElasticsearchService _elasticsearchService;
    private readonly ILogger<SearchProductsQueryHandler> _logger;
    private readonly IMapper _mapper;

    public SearchProductsQueryHandler(IElasticsearchService elasticsearchService, ILogger<SearchProductsQueryHandler> logger, IMapper mapper)
    {
        _elasticsearchService = elasticsearchService;
        _logger = logger;
        _mapper = mapper;
    }

    public async Task<Result<SearchResponseDto>> Handle(SearchProductsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var (products, totalCount) = await _elasticsearchService.SearchProductsAsync(
                request.Query,
                request.Category,
                request.MinPrice,
                request.MaxPrice,
                request.InStock,
                request.Page,
                request.PageSize,
                cancellationToken);
            _logger.LogInformation($"Search elasticsearch tooks products: {products}", products);

            if (!products.Any())
            {
                return Result<SearchResponseDto>.ResponseError(
                    code: ErrorCodes.NotFound,
                    message: "Products not found",
                    status: HttpStatusCode.NotFound
                );
            }
            
            var productsDto = _mapper.Map<List<ProductDto>>(products);

            var searchResponsDto = new SearchResponseDto
            {
                Products = productsDto,
                TotalCount = totalCount,
                Page = request.Page,
                PageSize = request.PageSize,
                TotalPages = (int)Math.Ceiling((double)totalCount / request.PageSize),
                Query = request.Query,
                Filters = new SearchFiltersDto
                {
                    Category = request.Category,
                    MinPrice = request.MinPrice,
                    MaxPrice = request.MaxPrice,
                    InStockOnly = request.InStock
                }
            };

            return Result<SearchResponseDto>.ResponseSuccess(searchResponsDto);

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"[{nameof(Handle)}] unexpected_error");
            return Result<SearchResponseDto>.ResponseError(
                code: ErrorCodes.InternalError,
                message: ErrorMessages.InternalServerError,
                status: HttpStatusCode.InternalServerError
            );
        }
    }
}

