using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Production.Domain.Entities;
using ProductEntity = Production.Domain.Entities.Product;

namespace Production.Application.Commons.Interfaces;

public interface IElasticsearchService
{
    Task InitialIndexAsync(CancellationToken cancellationToken = default);
    Task IndexProductAsync(ProductEntity product, CancellationToken cancellationToken = default);
    Task BulkIndexProductsAsync(IEnumerable<ProductEntity> products, CancellationToken cancellationToken = default);
    Task BulkIndexProductDocumentAsync(IEnumerable<Domain.Entities.ProductDocument>products, CancellationToken cancellationToken = default);
    Task<(List<ProductDocument> Products, long TotalCount)> SearchProductsAsync(
    string? searchTerm,
    string? category,
    decimal? minPrice,
    decimal? maxPrice,
    bool? inStockOnly,
    int page = 1,
    int pageSize = 20,
    CancellationToken cancellationToken = default);

    Task<(List<ProductSearchResult> Products, long totalCount)> HyridSearchProductsAsync(string keyword, float[] queryVector);
    Task<List<string>> GetSuggestionsAsync(string prefix, int limit = 10, CancellationToken cancellationToken = default);
    Task DeleteProductAsync(Guid productId, CancellationToken cancellationToken = default);
    Task UpdateProductAsync(ProductEntity product, CancellationToken cancellationToken = default);
    Task<Dictionary<string, long>> GetCategoryAggregationsAsync(CancellationToken cancellationToken = default);
}

