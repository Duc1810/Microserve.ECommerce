using BuildingBlocks.Caching.Configuaration;
using BuildingBlocks.Caching.Services;
using BuildingBlocks.EFCore;
using BuildingBlocks.Messaging.MassTransit;
using BuildingBlocks.Repository;
using Elastic.Clients.Elasticsearch;
using Google.Protobuf.WellKnownTypes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using product.Infrastructure.Data;
using Production.Application.Commons.Interfaces;
using Production.Application.Commons.Options;
using Production.Application.Features.Consumers.OrderCosumer;
using Production.Infrastructure.Data;
using Production.Infrastructure.Data.Configurations;
using Production.Infrastructure.Services;


namespace Production.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.AddCustomDbContext<ApplicationDbContext>(configuration);
            //services.AddScoped<IProductReadReposito, ProductReadRepository>();
            services.AddScoped<CatalogInitialData>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<DbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());
            services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
            services.AddRedisConfiguration(configuration);
            services.AddSingleton<IVersionStore, RedisVersionStore>();
            services.AddScoped<Production.Application.Commons.Interfaces.ICacheService, RedisCacheService>();
            services.AddScoped<IElasticsearchService, ElasticsearchService>();

            var elasticsearchUrl = configuration.GetConnectionString("Elasticsearch")
            ?? "http://localhost:9200";
            services.AddSingleton(sp =>
            {
                var settings = new ElasticsearchClientSettings(new Uri(elasticsearchUrl))
                    .DefaultIndex("products");
                return new ElasticsearchClient(settings);
            });

            services.AddMessageBroker(
                configuration,
                typeof(ReserveStockConsumer).Assembly
            );
            // register ollama service with http client factory
            services.Configure<OllamaOptions>(configuration.GetSection("Ollama"));
            services.AddHttpClient<IOllamaService, OllamaService>()
            .ConfigureHttpClient((sp, client) =>
            {
                var options = sp.GetRequiredService<IOptions<OllamaOptions>>().Value;

                client.BaseAddress = new Uri(options.BaseUrl);
                client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
            });

            //data seeder
            services.AddScoped<ProductSeeder>();
           

            return services;
        }

        public static async Task SeedAsync(this IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var initial = scope.ServiceProvider.GetRequiredService<CatalogInitialData>();
            await initial.PopulateAsync();
        }
    }
}

