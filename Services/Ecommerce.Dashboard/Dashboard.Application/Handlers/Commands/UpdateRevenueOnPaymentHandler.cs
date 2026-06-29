using BuildingBlocks.CQRS;
using Dashboard.Application.Abstractions;
using Dashboard.Application.Commands;
using Microsoft.Extensions.Logging;
using System.Data;

namespace Dashboard.Application.Handlers.Commands;

public class UpdateRevenueOnPaymentHandler : ICommandHandler<UpdateRevenueOnPaymentCommand>
{
    private readonly IDashboardRepository _repository;
    private readonly ILogger<UpdateRevenueOnPaymentHandler> _logger;

    public UpdateRevenueOnPaymentHandler(
        IDashboardRepository repository,
        ILogger<UpdateRevenueOnPaymentHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<Unit> Handle(UpdateRevenueOnPaymentCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // 1. Insert event into EventStore with atomic idempotency check
            var rowsAffected = await _repository.InsertEventIfNotExistsAsync(
                request.EventId, 
                "PaymentCompleted", 
                System.Text.Json.JsonSerializer.Serialize(request), 
                request.OccurredOn);

            if (rowsAffected == 0)
            {
                _logger.LogInformation("Duplicate event {EventId}, skipping processing", request.EventId);
                return Unit.Value; // Idempotent exit
            }

            // 2. Update DailyRevenueSummary
            var date = request.OccurredOn.Date;
            await _repository.UpsertDailyRevenueSummaryAsync(date, request.Amount, 1);

            // 3. Update OrderStateRecord
            await _repository.UpsertOrderStateAsync(request.OrderId, "Completed", request.OccurredOn);

            // 4. Update TopProductSnapshot for each item
            foreach (var item in request.Items)
            {
                var revenue = item.UnitPrice * item.Quantity;
                await _repository.UpsertTopProductAsync(
                    item.ProductId, 
                    item.ProductName, 
                    item.Quantity, 
                    revenue, 
                    request.OccurredOn);
            }

            _logger.LogInformation("Successfully updated revenue data for order {OrderId} with amount {Amount}", 
                request.OrderId, request.Amount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update revenue data for order {OrderId}", request.OrderId);
            throw;
        }

        return Unit.Value;
    }
}