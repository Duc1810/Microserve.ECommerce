using Production.Application.Commons.Interfaces;
using Production.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Production.Application.Features.Queries.GetSugestproduct;
public class GetSuggestionQueryHandler : IQueryHandler<GetSuggestionsQuery, Result<List<string>>>
{
    private readonly IElasticsearchService _elasticsearchService;
    private readonly ILogger<GetSuggestionQueryHandler> _logger;

    public GetSuggestionQueryHandler(IElasticsearchService elasticsearchService, ILogger<GetSuggestionQueryHandler> logger)
    {
        _elasticsearchService = elasticsearchService;
        _logger = logger;
    }
    public async Task<Result<List<string>>> Handle(GetSuggestionsQuery request, CancellationToken cancellationToken)
    {
        var productsName = await _elasticsearchService.GetSuggestionsAsync(request.Prefix, request.Limit, cancellationToken);
        _logger.LogInformation($"Search elasticsearch tooks products: {productsName}");
        return Result<List<string>>.ResponseSuccess(productsName);
    }
}

