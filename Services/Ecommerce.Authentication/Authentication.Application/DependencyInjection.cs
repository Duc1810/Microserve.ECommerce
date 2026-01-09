using BuildingBlocks.Observability.Behaviors;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Authentication.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            var appAssembly = Assembly.Load("Authentication.Application");

            services.AddMediatR(cfg =>
            {
                cfg.RegisterServicesFromAssembly(appAssembly);
                cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
                cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
            });

            services.AddValidatorsFromAssembly(appAssembly);

            return services;
        }
    }
}
