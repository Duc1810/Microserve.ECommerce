using BuildingBlocks.Caching.Configuaration;
using BuildingBlocks.Caching.Services;
using BuildingBlocks.EFCore;
using BuildingBlocks.Messaging.MassTransit;
using BuildingBlocks.Repository;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using product.Infrastructure.Data;
using Production.Application.Abstractions;
using Production.Application.Features.Consumers.OrderCosumer;
using Production.Infrastructure.Data.Configurations;


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

            services.AddMessageBroker(
                configuration,
                typeof(OrderCreatedConsumer).Assembly
            );
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

