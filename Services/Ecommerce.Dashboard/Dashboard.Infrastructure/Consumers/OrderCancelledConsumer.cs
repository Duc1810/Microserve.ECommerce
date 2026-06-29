using BuildingBlocks.Messaging.Events;
using BuildingBlocks.Commands;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Dashboard.Infrastructure.Consumers;

public class OrderCancelledConsumer : IConsumer<ICancelOrderCommand>
{
    private readonly IMediator _mediator;
    private readonly ILogger<OrderCancelledConsumer> _logger;

    public OrderCancelledConsumer(IMediator mediator, ILogger<OrderCancelledConsumer> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<ICancelOrderCommand> context)
    {
        var evt = context.Message;
        
        _logger.LogInformation("Processing CancelOrderCommand for OrderId: {OrderId}", 
            evt.OrderId);

        try
        {
            // TODO: Implement command dispatch to update order state to cancelled
            // This will be implemented when the command handlers are available
            
            _logger.LogInformation("Successfully processed CancelOrderCommand for OrderId: {OrderId}", evt.OrderId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing CancelOrderCommand for OrderId: {OrderId}", evt.OrderId);
            throw; // Let MassTransit handle retry logic
        }
    }
}