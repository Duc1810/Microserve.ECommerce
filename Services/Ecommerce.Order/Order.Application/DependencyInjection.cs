using AutoMapper;
using BuildingBlocks.Messaging.MassTransit;
using BuildingBlocks.Observability.Behaviors;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Order.Application.Features.Commands.CreateOrder;
using System.Reflection;

namespace Order.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplicationServices
            (this IServiceCollection services, IConfiguration configuration)
        {

            services.AddScoped<IMapper>(sp =>
            {
                var config = new MapperConfiguration(cfg =>
                {
                    cfg.ShouldMapMethod = _ => false;      
                    cfg.AddProfile<OrderMappingProfile>();
                });
                config.AssertConfigurationIsValid();
                return config.CreateMapper();
            });

            services.AddValidatorsFromAssemblyContaining<CreateOrderCommandValidator>();
            services.AddMediatR(config =>
            {
                config.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
                config.AddOpenBehavior(typeof(ValidationBehavior<,>));
                config.AddOpenBehavior(typeof(LoggingBehavior<,>));
            });
            services.AddMessageBroker(configuration, Assembly.GetExecutingAssembly());

            return services;
        }
    }
}
