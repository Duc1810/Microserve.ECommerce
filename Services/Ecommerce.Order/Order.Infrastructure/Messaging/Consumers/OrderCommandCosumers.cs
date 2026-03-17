using BuildingBlocks.Commands;
using BuildingBlocks.Repository;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Order.Infrastructure.Messaging.Consumers;

public class OrderCommandCosumers : IConsumer<ICompleteOrderCommand>, IConsumer<ICancelOrderCommand>
{
    private readonly IUnitOfWork _unitOfRepository;
    private readonly ILogger<OrderCommandCosumers> _logger;

    public OrderCommandCosumers(IUnitOfWork unitOfRepository, ILogger<OrderCommandCosumers> logger)
    {
        _unitOfRepository = unitOfRepository;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<ICompleteOrderCommand> context)
    {
        _logger.LogInformation("Completing Order with CorrelationId {OrderId}", context.Message.OrderId);

        var order = await _unitOfRepository.GetRepository<Domain.Models.Order>().FindAsync(context.Message.OrderId);
        if(order != null)
        {
            order.UpdateStatus(Domain.Enums.OrderStatus.Completed);
            await _unitOfRepository.SaveAsync();
            _logger.LogInformation("Order {OrderId} status updated to Completed", order.Id);
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

