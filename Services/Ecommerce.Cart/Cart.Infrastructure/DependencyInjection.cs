using BuildingBlocks.Caching.Configuaration;
using BuildingBlocks.EFCore;
using BuildingBlocks.Observability.Exceptions;
using BuildingBlocks.Repository;
using Cart.Application.Abstractions;
using Cart.Infrastructure.Data;
using Cart.Infrastructure.Grpc.Clients;
using Cart.Infrastructure.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Extensions.Http;
using Refit;
using System.Text.Json;
using Proto = Cart.GrpcContracts;

namespace Cart.Infrastructure;

public static class DependencyInjection
{

    public static IServiceCollection AddPersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddCustomDbContext<ApplicationDbContext>(configuration);
        services.AddScoped<DbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
        services.AddHttpContextAccessor();

        services.AddTransient<AuthHandler>();
        services.AddRedisConfiguration(configuration);

        return services;
    }


    public static IServiceCollection AddProductGrpc(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<ProductGrpcOptions>()
             .Bind(configuration.GetSection(ProductGrpcOptions.SectionName))
             .ValidateDataAnnotations()
             .Validate(o => !string.IsNullOrWhiteSpace(o.ProductUrl),
                 "GrpcSettings:ProductUrl is missing.")
             .ValidateOnStart();

        services.AddGrpcClient<Proto.ProductService.ProductServiceClient>((sp, options) =>
        {
            var opts = sp.GetRequiredService<IOptions<ProductGrpcOptions>>().Value;
            options.Address = new Uri(opts.ProductUrl);
        })
        .ConfigurePrimaryHttpMessageHandler(sp =>
        {
            var env = sp.GetRequiredService<IHostEnvironment>();
            if (env.IsDevelopment())
            {
                var handler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback =
                        HttpClientHandler.DangerousAcceptAnyServerCertificateValidator,
                    ClientCertificateOptions = ClientCertificateOption.Manual
                };
                handler.ServerCertificateCustomValidationCallback = (_, __, ___, ____) => true;
                return handler;
            }

            return new HttpClientHandler();
        });


        services.AddScoped<IProductService, ProductGrpcClient>();

        return services;
    }

    public static IServiceCollection AddOrderHttpApi(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<OrderApiOptions>(configuration.GetSection(OrderApiOptions.SectionName));

        var retryPolicy = HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(msg => (int)msg.StatusCode == 429)
            .WaitAndRetryAsync(new[]
            {
                TimeSpan.FromMilliseconds(200),
                TimeSpan.FromMilliseconds(500),
                TimeSpan.FromSeconds(1)
            });

        var breakerPolicy = HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(5, TimeSpan.FromSeconds(30));

        services
            .AddRefitClient<IOrderApi>(new RefitSettings
            {
                ContentSerializer = new SystemTextJsonContentSerializer(new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    PropertyNameCaseInsensitive = true
                })
            })
            .ConfigureHttpClient((sp, c) =>
            {
                var opts = sp.GetRequiredService<IOptions<OrderApiOptions>>().Value;
                if (string.IsNullOrWhiteSpace(opts.BaseAddress))
                    throw new BadRequestException("OrderApi:BaseAddress is missing in configuration.");

                c.BaseAddress = new Uri(opts.BaseAddress);
                c.Timeout = TimeSpan.FromSeconds(opts.TimeoutSeconds);
            })
            .AddHttpMessageHandler<AuthHandler>()
            .AddPolicyHandler(retryPolicy)
            .AddPolicyHandler(breakerPolicy);

        return services;
    }

    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddPersistence(configuration)
            .AddProductGrpc(configuration)
            .AddOrderHttpApi(configuration);

        return services;
    }
}
