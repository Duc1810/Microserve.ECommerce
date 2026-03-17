using Production.Application.Commons.Interfaces;

namespace Production.Application.Features.Queries.HyridSearchProduct;
public class HyridSearchQueryHandler : IQueryHandler<HyridSearchProductsQuery, Result<SearchResponseDto>>
{
    private readonly IElasticsearchService _elasticsearchService;
    private readonly IOllamaService _ollamaService;
    private readonly IMapper _mapper;
    private readonly ILogger<HyridSearchQueryHandler> _logger;

    public HyridSearchQueryHandler(IElasticsearchService elasticsearchService, ILogger<HyridSearchQueryHandler> logger, IOllamaService ollamaService, IMapper mapper)
    {
        _elasticsearchService = elasticsearchService;
        _logger = logger;
        _ollamaService = ollamaService;
        _mapper = mapper;
    }

    public async Task<Result<SearchResponseDto>> Handle(HyridSearchProductsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            // create embedding vector for the keyword using Ollama API
            var queryVector = await _ollamaService.GetVectorAsync(request.Keyword, cancellationToken);
            if (queryVector.Any())
            {

                // perform hybrid search using Elasticsearch with the query vector and keyword
                var (products, totalCount) = await _elasticsearchService.HyridSearchProductsAsync(request.Keyword, queryVector.ToArray());
                _logger.LogInformation($"Hybrid search for keyword '{request.Keyword}' returned {products.Count} products.");
                if (!products.Any())
                {
                    return Result<SearchResponseDto>.ResponseError(
                        code: ErrorCodes.NotFound,
                        message: "Products not found",
                        status: HttpStatusCode.NotFound
                    );
                }
                var productsDto = _mapper.Map<List<ProductSearchItemDto>>(products);
                var searchResponsDto = new SearchResponseDto
                {
                    Products = productsDto,
                    TotalCount = totalCount,
                    Query = request.Keyword
                };
                return Result<SearchResponseDto>.ResponseSuccess(searchResponsDto);
            }
            else
            {
                _logger.LogWarning($"Failed to get embedding vector for keyword: {request.Keyword}");
                return Result<SearchResponseDto>.ResponseError(
                    code: ErrorCodes.BadRequest,
                    message: "Unable to process the search query. Please try again with a different keyword.",
                    status: HttpStatusCode.BadRequest);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while performing hybrid search for products with keyword: {Keyword}", request.Keyword);
            return Result<SearchResponseDto>.ResponseError(
                code: ErrorCodes.InternalError,
                message: "An error occurred while processing your request. Please try again later.",
                status: HttpStatusCode.InternalServerError);
        }
    }
}

