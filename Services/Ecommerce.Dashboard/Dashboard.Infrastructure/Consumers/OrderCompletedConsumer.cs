using BuildingBlocks.Commands;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;
using Dashboard.Application.Commands;

namespace Dashboard.Infrastructure.Consumers;

public class OrderCompletedConsumer : IConsumer<IOrderCompletedEvent>
{
    private readonly IMediator _mediator;
    private readonly ILogger<OrderCompletedConsumer> _logger;

    public OrderCompletedConsumer(IMediator mediator, ILogger<OrderCompletedConsumer> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<IOrderCompletedEvent> context)
    {
        var evt = context.Message;
        
        _logger.LogInformation("Processing OrderCompletedEvent for OrderId: {OrderId}, Amount: {Amount}", 
            evt.OrderId, evt.TotalAmount);

        try
        {
            // Convert OrderCompletedItemDto to Dashboard.Application.DTOs.OrderItemDto
            var dashboardItems = evt.Items.Select(item => new Dashboard.Application.DTOs.OrderItemDto(
                item.ProductId,
                "Unknown", // ProductName not available in Order service DTO
                item.Quantity,
                item.Price
            )).ToList();

            var command = new UpdateRevenueOnPaymentCommand(
                EventId: Guid.NewGuid(),
                OrderId: evt.OrderId,
                Amount: evt.TotalAmount,
                OccurredOn: evt.CompletedAt,
                Items: dashboardItems
            );

            await _mediator.Send(command);
            
            _logger.LogInformation("Successfully processed OrderCompletedEvent for OrderId: {OrderId}, Amount: {Amount}", 
                evt.OrderId, evt.TotalAmount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing OrderCompletedEvent for OrderId: {OrderId}", evt.OrderId);
            throw; // Let MassTransit handle retry logic
        }
    }
}