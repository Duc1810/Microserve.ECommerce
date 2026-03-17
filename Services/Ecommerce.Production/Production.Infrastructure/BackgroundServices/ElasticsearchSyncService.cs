using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using product.Infrastructure.Data;
using Production.Application.Commons.Interfaces;
using Production.Domain.Entities;
using Production.Infrastructure.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Production.Infrastructure.BackgroundServices
{
    public class ElasticsearchSyncService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ElasticsearchSyncService> _logger;
        private readonly TimeSpan _syncInterval = TimeSpan.FromMinutes(30); // Sync every hour
        private readonly IOllamaService _ollamaService;

        public ElasticsearchSyncService(
            IServiceProvider serviceProvider,
            ILogger<ElasticsearchSyncService> logger,
            IOllamaService ollamaService)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _ollamaService = ollamaService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Elasticsearch Sync Service is starting");

            // Wait for 1 minute before first sync to allow services to initialize
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {

                try
                {
                    await SyncElasticsearchAsync(stoppingToken);
                    _logger.LogInformation(
                        "Next Elasticsearch sync scheduled in {Minutes} minutes",
                        _syncInterval.TotalMinutes);
                    await Task.Delay(_syncInterval, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("Elasticsearch Sync Service is stopping");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred during Elasticsearch sync");
                    // Wait before retrying on error
                    await Task.Delay(TimeSpan.FromMinutes(10), stoppingToken);
                }
            }
        }

        private async Task SyncElasticsearchAsync(CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var elasticsearchService = scope.ServiceProvider.GetRequiredService<IElasticsearchService>();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            _logger.LogInformation("Starting Elasticsearch sync...");
            try
            {
                var documents = new List<ProductDocument>();

                // Get all products from the database
                var products = await dbContext.Products
                .AsNoTracking()
                .ToListAsync(cancellationToken);

                // Map products to documents with vector embeddings in parallel for efficiency
                foreach (var product in products)
                {
                    var doc = await MapToDocumentWithVectorAsync(product, cancellationToken);
                    documents.Add(doc);
                }

                // Sync to Elasticsearch (using bulk indexing for efficiency)
                if (products.Any())
                {
                    await elasticsearchService.BulkIndexProductDocumentAsync(documents, cancellationToken);
                    _logger.LogInformation("Elasticsearch sync completed successfully");
                }
                else
                {
                    _logger.LogInformation("No products found to sync with Elasticsearch");
                }

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during Elasticsearch sync");
                throw; // Rethrow to trigger retry logic in ExecuteAsync
            }
        }

        private async Task<ProductDocument> MapToDocumentWithVectorAsync(
    Domain.Entities.Product product,
    CancellationToken ct)
        {

            var doc = ProductDocument.FromProduct(product);

            var vector = await _ollamaService.GetVectorAsync(doc.SemanticText, ct);

            doc.Vector = vector;

            return doc;
        }
    }
}
