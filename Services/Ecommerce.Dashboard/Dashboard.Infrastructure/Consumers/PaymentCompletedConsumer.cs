using BuildingBlocks.Messaging.Events;
using BuildingBlocks.Commands;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;
using Dashboard.Application.Commands;
using Dashboard.Application.DTOs;

namespace Dashboard.Infrastructure.Consumers;

public class PaymentCompletedConsumer : IConsumer<IPaymentCompletedEvent>
{
    private readonly IMediator _mediator;
    private readonly ILogger<PaymentCompletedConsumer> _logger;

    public PaymentCompletedConsumer(IMediator mediator, ILogger<PaymentCompletedConsumer> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<IPaymentCompletedEvent> context)
    {
        var evt = context.Message;
        
        _logger.LogInformation("Processing PaymentCompletedEvent for OrderId: {OrderId}, TransactionId: {TransactionId}", 
            evt.OrderId, evt.TransactionId);

        try
        {
            // Convert BuildingBlocks.Commands.OrderItemDto to Dashboard.Application.DTOs.OrderItemDto
            var dashboardItems = evt.Items.Select(item => new Dashboard.Application.DTOs.OrderItemDto(
                Guid.TryParse(item.ProductId, out var productId) ? productId : Guid.Empty,
                "Unknown", // ProductName not available in event
                item.Quantity,
                item.UnitPrice
            )).ToList();

            var command = new UpdateRevenueOnPaymentCommand(
                EventId: Guid.NewGuid(), 
                OrderId: evt.OrderId,
                Amount: evt.Amount,
                OccurredOn: DateTime.UtcNow,
                Items: dashboardItems
            );

            await _mediator.Send(command);
            
            _logger.LogInformation("Successfully processed PaymentCompletedEvent for OrderId: {OrderId}, Amount: {Amount}", 
                evt.OrderId, evt.Amount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing PaymentCompletedEvent for OrderId: {OrderId}", evt.OrderId);
            throw; // Let MassTransit handle retry logic
        }
    }
}