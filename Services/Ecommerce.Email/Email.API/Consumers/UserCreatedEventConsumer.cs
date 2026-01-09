using BuildingBlocks.Messaging.Events;
using Email.API.Services;
using MassTransit;
using RabbitMQ.Client;

namespace Email.API.Consumers
{
    public class UserCreatedEventConsumer : IConsumer<UserCreatedEvent>
    {
        private readonly IEmailService _email;

        public UserCreatedEventConsumer(IEmailService email)
        {
            _email = email;
        }

        public async Task Consume(ConsumeContext<UserCreatedEvent> context)
        {
            var m = context.Message;

            await _email.SendVerifyEmailAsync(
                m.Email,
                m.Token,
                m.Href,
                m.Title,
                m.Message
            );
        }
    }
    public sealed class UserCreatedEventConsumerDefinition
        : ConsumerDefinition<UserCreatedEventConsumer>
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<UserCreatedEventConsumerDefinition> _logger;

        public UserCreatedEventConsumerDefinition(
            IConfiguration configuration,
            ILogger<UserCreatedEventConsumerDefinition> logger)
        {
            _configuration = configuration;
            _logger = logger;

            Endpoint(e =>
            {
                e.Name = "email-user-created-direct-queue";
            });
        }

        protected override void ConfigureConsumer(
            IReceiveEndpointConfigurator endpointConfigurator,
            IConsumerConfigurator<UserCreatedEventConsumer> consumerConfigurator)
        {
            if (endpointConfigurator is IRabbitMqReceiveEndpointConfigurator rmq)
            {
                rmq.ConfigureConsumeTopology = false;

                var routingKey = _configuration["MessageBroker:RoutingKeys:UserCreated"];
                if (string.IsNullOrWhiteSpace(routingKey))
                    throw new InvalidOperationException(
                        "Missing config: MessageBroker:RoutingKeys:UserCreated");

                _logger.LogInformation(
                    "Binding direct for UserCreatedEvent with routingKey={Key}", routingKey);

                 rmq.Bind("user-created", b =>
        {
            b.ExchangeType = ExchangeType.Direct;
            b.RoutingKey   = routingKey!;
        });
            }
        }
    }
}
