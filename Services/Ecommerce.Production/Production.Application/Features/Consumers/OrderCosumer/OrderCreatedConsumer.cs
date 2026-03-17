using BuildingBlocks.Caching.Services;
using BuildingBlocks.Messaging.Events;
using MassTransit;
using Dapper;
using BuildingBlocks.Commands;
using Microsoft.AspNetCore.WebUtilities;
namespace Production.Application.Features.Consumers.OrderCosumer;

public class OrderCreatedConsumer : IConsumer<CreatedEvent>
{
    private readonly IUnitOfWork _unitOfRepository;
    private readonly IVersionStore _redisVersionService;
    private readonly ILogger<OrderCreatedConsumer> _logger;
    public OrderCreatedConsumer(IUnitOfWork unitOfRepository, IVersionStore redisVersionService, ILogger<OrderCreatedConsumer> logger)
    {
        _unitOfRepository = unitOfRepository;
        _redisVersionService = redisVersionService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<CreatedEvent> context)
    {
        var createdOrderEvent = context.Message;
        var sortedItems = createdOrderEvent.Items.OrderBy(i => i.ProductId).ToList();

        _logger.LogInformation("Processing stock reservation for Order: {OrderId}", createdOrderEvent.OrderId);
        await _unitOfRepository.BeginTransactionAsync();
        try
        {
            var connection = _unitOfRepository.GetDbConnection();
            var dbTransaction = _unitOfRepository.GetCurrentdTransaction();

            const string updateSql = @"
            UPDATE Products 
            SET Quantity = Quantity - @Qty 
            WHERE Id = @Id AND (Quantity - @Qty >= 0)";

            foreach (var item in sortedItems)
            {
                var affectedRows = await connection.ExecuteAsync(updateSql, new
                {
                    Qty = item.Quantity,
                    Id = item.ProductId
                }, (System.Data.IDbTransaction?)dbTransaction);

                if (affectedRows == 0)
                {
                    _logger.LogWarning("Insufficient stock for Product {ProductId}", item.ProductId);
                    await context.Publish<IStockReservationFailedEvent>(new
                    {
                        OrderId = createdOrderEvent.OrderId,
                        Reason = $"Insufficient stock for Product {item.ProductId}"
                    });
                    return;
                }
            }

            await _unitOfRepository.CommitTransactionAsync();
            _logger.LogInformation("Stock reserved for Order {OrderId}", context.Message.OrderId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reserving stock for Order {OrderId}", context.Message.OrderId);
            await _unitOfRepository.RollbackTransactionAsync();
            await context.Publish<IStockReservationFailedEvent>(new
            {
                OrderId = context.Message.OrderId,
                Reason = "Internal error during stock reservation"
            });
            throw;
        }
    }
}

