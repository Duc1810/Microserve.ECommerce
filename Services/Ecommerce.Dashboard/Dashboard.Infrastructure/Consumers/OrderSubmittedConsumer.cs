using BuildingBlocks.Messaging.Events;
using BuildingBlocks.Commands;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Dashboard.Infrastructure.Consumers;

public class OrderSubmittedConsumer : IConsumer<IOrderSubmittedEvent>
{
    private readonly IMediator _mediator;
    private readonly ILogger<OrderSubmittedConsumer> _logger;

    public OrderSubmittedConsumer(IMediator mediator, ILogger<OrderSubmittedConsumer> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<IOrderSubmittedEvent> context)
    {
        var evt = context.Message;
        
        _logger.LogInformation("Processing OrderSubmittedEvent for OrderId: {OrderId}", 
            evt.OrderId);

        try
        {
            // TODO: Implement command dispatch to update order state
            // This will be implemented when the command handlers are available
            
            _logger.LogInformation("Successfully processed OrderSubmittedEvent for OrderId: {OrderId}", evt.OrderId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing OrderSubmittedEvent for OrderId: {OrderId}", evt.OrderId);
            throw; // Let MassTransit handle retry logic
        }
    }
}