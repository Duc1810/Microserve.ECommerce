using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.Aggregations;
using Elastic.Clients.Elasticsearch.Core.Search;
using Elastic.Clients.Elasticsearch.QueryDsl;
using Elasticsearch.Net;
using Microsoft.Extensions.Logging;
using product.Infrastructure.Data;
using Production.Application.Commons.Interfaces;
using Production.Application.Dtos.Products;
using Production.Domain.Entities;

namespace Production.Infrastructure.Services;

public class ElasticsearchService : IElasticsearchService
{
    private readonly ElasticsearchClient _client;
    private readonly ILogger<ElasticsearchService> _logger;
    private const string IndexName = "products";
    private const int RankConstant = 60;

    public ElasticsearchService(ElasticsearchClient client, ILogger<ElasticsearchService> logger)
    {
        _client = client;
        _logger = logger;
    }
    public async Task InitialIndexAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Check if index exists, if not create it with appropriate mappings
            var existsResponse = await _client.Indices.ExistsAsync(IndexName, cancellationToken);

            if (existsResponse.Exists)
            {
                _logger.LogInformation("Elasticsearch index '{IndexName}' already exists", IndexName);
                return;
            }

            var createResponse = await _client.Indices.CreateAsync(IndexName, c => c
                .Mappings(m => m
                    .Properties<ProductDocument>(p => p
                        .Keyword(k => k.Id)
                        .Text(t => t.Name!)
                        .Text(t => t.Description!)
                        .DoubleNumber(n => n.Price)
                        .IntegerNumber(n => n.Quantity)
                        .Keyword(k => k.Category!)
                        .Boolean(b => b.InStock)
                        .Date(d => d.IndexedAt)
                       .DenseVector(v => v.Vector!, dv => dv
                            .Dims(768)
                            .Index(true)
                            .Similarity("cosine")
                       )
                       .Text(t => t.SemanticText!)
                    )
                ), cancellationToken);
            if (createResponse.IsValidResponse)
            {
                _logger.LogInformation("Successfully created Elasticsearch index '{IndexName}'", IndexName);
            }
            else
            {
                _logger.LogError("Failed to create Elasticsearch index: {Error}", createResponse.DebugInformation);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing Elasticsearch index");
            throw;
        }
    }

    public async Task IndexProductAsync(Domain.Entities.Product product, CancellationToken cancellationToken = default)
    {
        try
        {
            var document = ProductDocument.FromProduct(product);
            var response = await _client.IndexAsync(document, idx => idx.Index(IndexName), cancellationToken);

            if (!response.IsValidResponse)
            {
                _logger.LogWarning("Failed to index product {ProductId}: {Error}",
                    product.Id, response.DebugInformation);
            }
            else
            {
                _logger.LogDebug("Successfully indexed product {ProductId}", product.Id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error indexing product {ProductId}", product.Id);
        }
    }

    public async Task BulkIndexProductsAsync(IEnumerable<Domain.Entities.Product> products, CancellationToken cancellationToken = default)
    {
        try
        {
            var documents = products.Select(p => ProductDocument.FromProduct(p, null)).ToList();

            if (!documents.Any())
            {
                _logger.LogInformation("No products to index");
                return;
            }

            var bulkResponse = await _client.BulkAsync(b => b
                .Index(IndexName)
                .IndexMany(documents), cancellationToken);

            if (bulkResponse.IsValidResponse)
            {
                _logger.LogInformation("Successfully bulk indexed {Count} products", documents.Count);
            }
            else
            {
                _logger.LogWarning("Bulk index had errors: {Error}", bulkResponse.DebugInformation);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error bulk indexing products");
        }
    }


    public async Task BulkIndexProductDocumentAsync(IEnumerable<ProductDocument> products, CancellationToken cancellationToken = default)
    {
        try
        {

            if (!products.Any())
            {
                _logger.LogInformation("No products to index");
                return;
            }

            var bulkResponse = await _client.BulkAsync(b => b
                .Index(IndexName)
                .IndexMany(products), cancellationToken);

            if (bulkResponse.IsValidResponse)
            {
                _logger.LogInformation("Successfully bulk indexed {Count} products", products.Count());
            }
            else
            {
                _logger.LogWarning("Bulk index had errors: {Error}", bulkResponse.DebugInformation);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error bulk indexing products");
        }
    }

    public async Task<(List<ProductDocument> Products, long TotalCount)> SearchProductsAsync(
        string? searchTerm,
        string? category,
        decimal? minPrice,
        decimal? maxPrice,
        bool? inStockOnly,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        try
        {

            var mustQueries = new List<Query>();

            // Full-text search on name and description
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                mustQueries.Add(new MultiMatchQuery
                {
                    Query = searchTerm,
                    Fields = new[] { "name^2", "description" }, // Boost name field
                    Fuzziness = new Fuzziness("AUTO"),
                    Operator = Operator.Or
                });
            }

            // Category filter
            if (!string.IsNullOrWhiteSpace(category))
            {
                mustQueries.Add(new TermQuery("category"!) { Value = category });
            }

            //Price range filter
            if (minPrice.HasValue || maxPrice.HasValue)
            {
                var rangeQuery = new NumberRangeQuery("price"!);
                if (minPrice.HasValue) rangeQuery.Gte = (double)minPrice.Value;
                if (maxPrice.HasValue) rangeQuery.Lte = (double)maxPrice.Value;
                mustQueries.Add(rangeQuery);
            }

            // In-stock filter
            if (inStockOnly == true)
            {
                mustQueries.Add(new TermQuery("inStock"!) { Value = true });
            }

            var searchRequest = new SearchRequest(IndexName)
            {
                Query = mustQueries.Any()
                       ? new BoolQuery { Must = mustQueries }
                       : new MatchAllQuery(),
                From = (page - 1) * pageSize,
                Size = pageSize,
                Sort = new List<SortOptions>
                {
                    SortOptions.Score(new ScoreSort { Order = SortOrder.Desc }),
                    SortOptions.Field("name.keyword"!, new FieldSort { Order = SortOrder.Asc })
                }
            };

            var response = await _client.SearchAsync<ProductDocument>(searchRequest, cancellationToken);

            if (!response.IsValidResponse)
            {
                _logger.LogError("Elasticsearch search failed: {Error}", response.DebugInformation);
                return (new List<ProductDocument>(), 0);
            }

            var products = response.Documents.ToList();
            var totalCount = response.Total;

            _logger.LogInformation("Search returned {Count} products out of {Total}",
                products.Count, totalCount);

            return (products, totalCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching products in Elasticsearch");
            return (new List<ProductDocument>(), 0);
        }
    }

    public async Task<List<string>> GetSuggestionsAsync(string prefix, int limit = 10, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(prefix))
            {
                return new List<string>();
            }

            var sẻarchRequest = new SearchRequest(IndexName)
            {
                Query = new PrefixQuery("name.keyword"!) { Value = prefix! },
                Size = limit,
            };

            var response = await _client.SearchAsync<ProductDocument>(sẻarchRequest, cancellationToken);

            if (response.IsValidResponse)
            {
                var suggestions = response.Documents.Select(d => d.Name).Distinct().ToList();
                _logger.LogInformation("Suggestions for prefix '{Prefix}': {Suggestions}", prefix, suggestions);
                return suggestions;
            }

            return new List<string>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting suggestions from Elasticsearch for prefix '{Prefix}'", prefix);
            return new List<string>();
        }
    }

    public async Task DeleteProductAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _client.DeleteAsync<ProductDocument>(IndexName, productId.ToString(), cancellationToken);

            if (!response.IsValidResponse)
            {
                _logger.LogWarning("Failed to delete product {ProductId}: {Error}",
                    productId, response.DebugInformation);
            }
            else
            {
                _logger.LogInformation("Successfully deleted product {ProductId} from index", productId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting product {ProductId}", productId);
        }
    }

    public async Task UpdateProductAsync(Domain.Entities.Product product, CancellationToken cancellationToken = default)
    {
        // For simplicity, we'll re-index the product
        await IndexProductAsync(product, cancellationToken);
    }

    public async Task<Dictionary<string, long>> GetCategoryAggregationsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var searchRequest = new SearchRequest(IndexName)
            {
                Size = 0, // We only want aggregations
                Aggregations = new Dictionary<string, Aggregation>
                {
                    ["categories"] = new TermsAggregation
                    {
                        Field = "category",
                        Size = 100
                    }
                }
            };

            var response = await _client.SearchAsync<ProductDocument>(searchRequest, cancellationToken);

            if (!response.IsValidResponse || response.Aggregations == null)
            {
                _logger.LogWarning("Failed to get category aggregations: {Error}", response.DebugInformation);
                return new Dictionary<string, long>();
            }

            var categoriesAgg = response.Aggregations.GetStringTerms("categories");
            if (categoriesAgg == null)
            {
                return new Dictionary<string, long>();
            }

            return categoriesAgg.Buckets.ToDictionary(
                b => b.Key.ToString() ?? "Unknown",
                b => b.DocCount
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting category aggregations");
            return new Dictionary<string, long>();
        }
    }

    public async Task<(List<ProductSearchResult> Products, long totalCount)> HyridSearchProductsAsync(string keyword, float[] queryVector)
    {
        try
        {
            // 50 BM25 lexical search
            var bm25Task = _client.SearchAsync<ProductDocument>(s => s
                .Index("products")
                .Size(50)
                .Query(q => q
                .Bool(b => b
                .Should(sh => sh.MultiMatch(m => m
                     .Query(keyword)
                     .Type(TextQueryType.BestFields)
                     .Fields(new[]
                     {
                        "name^8",
                        "semanticText^5",
                        "category^8",
                        "description^2"
                     })
                     .Operator(Operator.Or)
                     .MinimumShouldMatch("75%")
                     .Fuzziness(new Fuzziness("AUTO"))
                    ),
                        sh => sh.MatchPhrase(mp => mp
                                .Field("name"!)
                                .Query(keyword)
                                .Boost(10)
                        )

                ))));

            //Run KNN
            var vectorTask = _client.SearchAsync<ProductDocument>(s => s
                                        .Index("products")
                                        .Size(50)
                                        .Knn(k => k
                                             .Field(f => f.Vector)
                                                .QueryVector(queryVector)
                                                .NumCandidates(200)
     )
 );

            await Task.WhenAll(bm25Task, vectorTask);

            var bm25Results = bm25Task.Result.Hits;
            var knnResults = vectorTask.Result.Hits;


            var filteredVectorHits = knnResults
            .Where(h => h.Score.HasValue && h.Score > 0.75)
            .ToList();

            // apply RRF
            var rrfScores = new Dictionary<string, double>();
            var documents = new Dictionary<string, ProductDocument>();

            CalculateRrfScores(bm25Results, rrfScores, documents);
            CalculateRrfScores(filteredVectorHits, rrfScores, documents);

            var ranked = rrfScores
            .OrderByDescending(x => x.Value)
            .Take(20)
            .ToList();

            double maxScore = ranked.FirstOrDefault().Value;

            var results = ranked.Select(x =>
            {
                var doc = documents[x.Key];

                return new ProductSearchResult
                {
                    Product = doc,
                    Score = Math.Round(x.Value, 5),
                    MatchPercent = Math.Round((x.Value / maxScore) * 100, 2)
                };
            }).ToList();


            if (!results.Any())
            {
                _logger.LogInformation("No products found for hybrid search with keyword '{Keyword}'", keyword);
            }

            return (results, results.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing hybrid search");
            return (new List<ProductSearchResult>(), 0L);
        }
    }
    private void CalculateRrfScores(
    IReadOnlyCollection<Elastic.Clients.Elasticsearch.Core.Search.Hit<ProductDocument>> hits,
    Dictionary<string, double> rrfScores,
    Dictionary<string, ProductDocument> documents)
    {
        const int rankConstant = 60;

        int rank = 1;

        foreach (var hit in hits)
        {
            var id = hit.Source!.Id.ToString();

            double score = 1.0 / (rankConstant + rank);

            if (rrfScores.ContainsKey(id))
                rrfScores[id] += score;
            else
                rrfScores[id] = score;

            if (!documents.ContainsKey(id))
                documents[id] = hit.Source!;

            rank++;
        }
    }
}

