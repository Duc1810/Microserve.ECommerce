using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks.Messaging
{
    public interface IEventBus
    {
        Task PublishAsync<T>(T evt, string routingKeyName) where T : class;
    }
    public class EventBus : IEventBus
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public EventBus(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        public async Task PublishAsync<T>(T evt, string routingKeyName) where T : class
        {
            using var scope = _scopeFactory.CreateScope();
            var sender = scope.ServiceProvider.GetRequiredService<ISendEndpointProvider>();
            var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
            var formatter = scope.ServiceProvider.GetService<IEndpointNameFormatter>();
            var routingKey = config[$"MessageBroker:RoutingKeys:{routingKeyName}"];
            if (string.IsNullOrWhiteSpace(routingKey))
                throw new InvalidOperationException($"Missing config: MessageBroker:RoutingKeys:{routingKeyName}");
            var exchangeName = formatter?.Message<T>() ?? typeof(T).Name;
            var endpoint = await sender.GetSendEndpoint(new Uri("exchange:user-created?type=direct"));
            await endpoint.Send(evt, ctx => ctx.SetRoutingKey(routingKey));
        }
    }
}
