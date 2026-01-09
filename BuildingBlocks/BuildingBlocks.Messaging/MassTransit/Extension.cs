using BuildingBlocks.Messaging.Events;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;
using System.Reflection;

namespace BuildingBlocks.Messaging.MassTransit
{
    public static class Extension
    {
        public static IServiceCollection AddMessageBroker(this IServiceCollection services, IConfiguration configuration, Assembly? assembly = null, Action<IBusRegistrationContext, IRabbitMqBusFactoryConfigurator>? configureBus = null)
        {
            services.AddMassTransit(config =>
            {
                config.SetKebabCaseEndpointNameFormatter();

                if (assembly != null)
                    config.AddConsumers(assembly);


                config.UsingRabbitMq((context, configurator) =>
                {
                    configurator.Host(new Uri(configuration["MessageBroker:Host"]!), host =>
                    {
                        host.Username(configuration["MessageBroker:UserName"]);
                        host.Password(configuration["MessageBroker:Password"]);
                    });
                    configurator.Message<UserCreatedEvent>(x => x.SetEntityName("user-created"));
                    configurator.Publish<UserCreatedEvent>(x =>
                    {
                        x.ExchangeType = ExchangeType.Direct;
                    });

                    if (configureBus is null)
                        configurator.ConfigureEndpoints(context);
                    else
                        configureBus(context, configurator);
                });
            });
            return services;
        }

        public static IServiceCollection AddEventBus(this IServiceCollection services)
        {
            services.AddSingleton<IEventBus, EventBus>();
            return services;
        }




    }
}
