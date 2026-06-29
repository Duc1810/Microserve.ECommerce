using BuildingBlocks.Commands;
using BuildingBlocks.Repository;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Order.Application.Dtos;

namespace Order.Infrastructure.Messaging.Consumers;

public class OrderCommandCosumers : IConsumer<ICompleteOrderCommand>, IConsumer<ICancelOrderCommand>
{
    private readonly IUnitOfWork _unitOfRepository;
    private readonly ILogger<OrderCommandCosumers> _logger;
    private readonly IPublishEndpoint _publishEndpoint;

    public OrderCommandCosumers(IUnitOfWork unitOfRepository, ILogger<OrderCommandCosumers> logger, IPublishEndpoint publishEndpoint)
    {
        _unitOfRepository = unitOfRepository;
        _logger = logger;
        _publishEndpoint = publishEndpoint;
    }

    public async Task Consume(ConsumeContext<ICompleteOrderCommand> context)
    {
        _logger.LogInformation("Completing Order with CorrelationId {OrderId}", context.Message.OrderId);

        var orderRepo = _unitOfRepository.GetRepository<Domain.Models.Order>();
        var order = await orderRepo.GetByPropertyAsync(
            filter: o => o.Id == context.Message.OrderId,
            includeProperties: "OrderItems"
        );

        if(order != null)
        {
            order.UpdateStatus(Domain.Enums.OrderStatus.Completed);
            await _unitOfRepository.SaveAsync();
            
            _logger.LogInformation("Order {OrderId} status updated to Completed", order.Id);

            // Publish OrderCompletedEvent for Dashboard service
            var orderItems = order.OrderItems.Select(item => new OrderCompletedItemDto(
                item.ProductId, 
                item.Quantity, 
                item.Price
            )).ToList();

            await _publishEndpoint.Publish<IOrderCompletedEvent>(new
            {
                OrderId = order.Id,
                TotalAmount = order.TotalPrice,
                Items = orderItems,
                CompletedAt = DateTime.UtcNow
            });

            _logger.LogInformation("OrderCompletedEvent published for OrderId: {OrderId}, Amount: {Amount}", 
                order.Id, order.TotalPrice);
        }
    }

    public async Task Consume(ConsumeContext<ICancelOrderCommand> context)
    {
        _logger.LogWarning("Cancelling Order with CorrelationId {OrderId}. Reason: {Reason}", context.Message.OrderId, context.Message.Reason);

        var order = await _unitOfRepository.GetRepository<Domain.Models.Order>().FindAsync(context.Message.OrderId);
        if (order != null)
        {
            order.UpdateStatus(Domain.Enums.OrderStatus.Cancelled);
            await _unitOfRepository.SaveAsync();
            _logger.LogInformation("Order {OrderId} status updated to Cancelled", order.Id);
        }
    }
}

