using BuildingBlocks.Commands;
using BuildingBlocks.Repository;
using MassTransit;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Production.Infrastructure.Messagings.Consumers
{
    public class ReleaseStockConsumer : IConsumer<IReleaseStockCommand>
    {
        private readonly IUnitOfWork _unitOfRepository;
        private readonly ILogger<ReleaseStockConsumer> _logger;

        public ReleaseStockConsumer(IUnitOfWork unitOfRepository, ILogger<ReleaseStockConsumer> logger)
        {
            _unitOfRepository = unitOfRepository;
            _logger = logger;
        }
        public async Task Consume(ConsumeContext<IReleaseStockCommand> context)
        {

            _logger.LogInformation(
            "Releasing reserved stock for Order {OrderId} (Compensating Transaction)",
            context.Message.OrderId);

            await _unitOfRepository.BeginTransactionAsync();
            try
            {
                var connection = _unitOfRepository.GetDbConnection();
                var dbTransaction = _unitOfRepository.GetCurrentdTransaction();
                foreach (var item in context.Message.Items)
                {
                    var product = await _unitOfRepository.GetRepository<Production.Domain.Entities.Product>().FindAsync(item.ProductId);
                    if (product != null)
                    {
                        // Restore the stock quantity
                        product.Quantity += item.Quantity;

                        _logger.LogInformation(
                            "Restored {Quantity} units of Product {ProductId} (New Stock: {NewStock})",
                            item.Quantity,
                            item.ProductId,
                            product.Quantity);
                    }
                    else
                    {
                        _logger.LogWarning(
                            "Product {ProductId} not found during stock release for Order {OrderId}",
                            item.ProductId,
                            context.Message.OrderId);
                    }
                }

                await _unitOfRepository.SaveAsync();
                await _unitOfRepository.CommitTransactionAsync();
                _logger.LogInformation("Stock released for Order {OrderId}", context.Message.OrderId);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error releasing stock for Order {OrderId}. Stock may not be restored.",
                    context.Message.OrderId);

                await _unitOfRepository.RollbackTransactionAsync();

                // Don't throw - this is a compensating transaction
                // Log the error for manual intervention if needed
                _logger.LogCritical(
                    "MANUAL INTERVENTION REQUIRED: Failed to release stock for Order {OrderId}. " +
                    "Stock quantities may be incorrect. Items: {Items}",
                    context.Message.OrderId,
                    System.Text.Json.JsonSerializer.Serialize(context.Message.Items));
            }
        }
    }
}
