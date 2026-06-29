using Dashboard.Application.Abstractions;
using Dashboard.Infrastructure.Data;
using Dashboard.Infrastructure.Repositories;
using Dashboard.Infrastructure.Services;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Dashboard.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Database Configuration
        services.AddDbContext<DashboardDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                b => b.MigrationsAssembly(typeof(DashboardDbContext).Assembly.FullName)));

        services.AddScoped<IDashboardDbContext>(provider => provider.GetRequiredService<DashboardDbContext>());

        // Infrastructure Services
        services.AddScoped<EventStoreService>();
        services.AddScoped<IDashboardRepository, DashboardRepository>();

        // MassTransit Configuration
        services.AddMassTransit(x =>
        {
            // Register consumers from this assembly
            x.AddConsumers(typeof(DependencyInjection).Assembly);

            x.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host(configuration.GetConnectionString("RabbitMQ"));

                // Configure retry policy
                cfg.UseMessageRetry(r => r.Exponential(
                    retryLimit: 5,
                    minInterval: TimeSpan.FromSeconds(1),
                    maxInterval: TimeSpan.FromMinutes(5),
                    intervalDelta: TimeSpan.FromSeconds(2)));

                cfg.ConfigureEndpoints(context);
            });
        });

        return services;
    }
}