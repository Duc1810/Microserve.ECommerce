using BuildingBlocks.Caching.Options;
using BuildingBlocks.Caching.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace BuildingBlocks.Caching.Configuaration
{
    public static class RedisConfig
    {
        public static IServiceCollection AddRedisConfiguration(this IServiceCollection services, IConfiguration configuration)
        {
            var redisOptions = new RedisOptions();
            configuration.GetSection(RedisOptions.OptionName).Bind(redisOptions);

            var redisUrl = $"{redisOptions.Host}:{redisOptions.Port}";
            var configurationOptions = new ConfigurationOptions
            {
                AbortOnConnectFail = false,
                Ssl = redisOptions.IsSSL,
                Password = redisOptions.Password
            };
            configurationOptions.EndPoints.Add(redisUrl);

            services.AddOptions<RedisOptions>()
                .Bind(configuration.GetSection(RedisOptions.OptionName))
                .Validate(o => !string.IsNullOrWhiteSpace(o.Host) && !string.IsNullOrWhiteSpace(o.Port),
                    "Redis Host/Port is required");

            services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(configurationOptions));
            services.AddSingleton<IVersionStore, RedisVersionStore>();
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redisUrl;
                options.ConfigurationOptions = configurationOptions;
            });

            services.AddScoped<IRedisService, RedisService>();

            return services;
        }
    }
}
