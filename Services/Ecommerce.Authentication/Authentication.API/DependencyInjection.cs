using BuildingBlocks.Identity;
using BuildingBlocks.Observability.Exceptions.Handler;
using BuildingBlocks.Observability.Swagger;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;

namespace Authentication.API.Configurations
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApiServices(this IServiceCollection services, IConfiguration config, IHostEnvironment env)
        {
            // Data protection
            var dpKeysPath = Path.Combine(env.ContentRootPath, "dp_keys");
            Directory.CreateDirectory(dpKeysPath);

            services.AddDataProtection()
                .PersistKeysToFileSystem(new DirectoryInfo(dpKeysPath))
                .SetApplicationName("AuthServerDev");

            // Forwarded headers
            services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            });

            // Controllers & API tools
            services.AddControllers();
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen();
            services.AddCustomSwagger();

            // Observability
            services.AddExceptionHandler<CustomExceptionHandler>();
            services.AddProblemDetails();
            services.AddCurrentUser();

            return services;
        }
    }
}
