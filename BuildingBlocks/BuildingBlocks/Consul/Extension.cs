using System;
using Consul;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace BuildingBlocks.Consul;

public static class ConsulServiceCollectionExtensions
{
    public static IServiceCollection AddConsulRegistration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<ConsulOptions>()
            .Bind(configuration.GetSection(nameof(ConsulOptions)))
            .Validate(o => !string.IsNullOrWhiteSpace(o.Name), "ConsulOptions.Name is required")
            .Validate(o => o.DiscoveryAddress is not null, "ConsulOptions.DiscoveryAddress is required")
            .Validate(o =>
            {

                return !string.IsNullOrWhiteSpace(o.HealthCheckEndPoint)
                       && !o.HealthCheckEndPoint.Contains("://");
            }, "ConsulOptions.HealthCheckEndPoint must be a relative path")
            .ValidateOnStart();

        services.AddSingleton<IConsulClient>(sp =>
        {
            var opts = sp.GetRequiredService<IOptions<ConsulOptions>>().Value;
            return new ConsulClient(c => c.Address = opts.DiscoveryAddress);
        });

        services.AddHostedService<ConsulHostedService>();
        return services;
    }
}
