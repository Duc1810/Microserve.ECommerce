using System.Diagnostics;
using System.Text.Json;
using BuildingBlocks.CQRS;
using Dashboard.Application.Abstractions;
using Dashboard.Application.Commands;
using Dashboard.Application.DTOs;
using Microsoft.Extensions.Logging;

namespace Dashboard.Application.Handlers.Commands;

public class RebuildMaterializedViewsHandler : ICommandHandler<RebuildMaterializedViewsCommand, RebuildResult>
{
    private readonly IDashboardRepository _repository;
    private readonly ILogger<RebuildMaterializedViewsHandler> _logger;

    public RebuildMaterializedViewsHandler(
        IDashboardRepository repository,
        ILogger<RebuildMaterializedViewsHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<RebuildResult> Handle(RebuildMaterializedViewsCommand request, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var processedCount = 0;

        try
        {
            _logger.LogInformation("Starting materialized views rebuild");

            // Clear all materialized views (do NOT delete EventStore)
            await _repository.TruncateAllMaterializedViewsAsync();

            // Replay events ordered by SequenceNumber
            var events = await _repository.GetAllEventsOrderedAsync();

            _logger.LogInformation("Found {EventCount} events to replay", events.Count());

            foreach (var storedEvent in events)
            {
                try
                {
                    await ApplyEvent(storedEvent);
                    processedCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to apply event {EventId} of type {EventType}", 
                        storedEvent.Id, storedEvent.EventType);
                    // Continue processing other events
                }
            }

            stopwatch.Stop();
            
            _logger.LogInformation("Completed materialized views rebuild. Processed {ProcessedCount} events in {Duration}ms", 
                processedCount, stopwatch.ElapsedMilliseconds);

            return new RebuildResult(processedCount, stopwatch.Elapsed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to rebuild materialized views");
            throw;
        }
    }

    private async Task ApplyEvent(Dashboard.Domain.Entities.DashboardEventStore storedEvent)
    {
        switch (storedEvent.EventType)
        {
            case "PaymentCompleted":
                await ApplyPaymentCompleted(storedEvent);
                break;
            case "OrderSubmitted":
                await ApplyOrderSubmitted(storedEvent);
                break;
            case "OrderCancelled":
                await ApplyOrderCancelled(storedEvent);
                break;
            default:
                _logger.LogWarning("Unknown event type: {EventType}", storedEvent.EventType);
                break;
        }
    }

    private async Task ApplyPaymentCompleted(Dashboard.Domain.Entities.DashboardEventStore storedEvent)
    {
        try
        {
            // Deserialize the payload to extract event data
            var payload = JsonSerializer.Deserialize<JsonElement>(storedEvent.Payload);
            
            // Extract data from payload (this would match the actual event structure)
            if (payload.TryGetProperty("OrderId", out var orderIdElement) &&
                payload.TryGetProperty("Amount", out var amountElement) &&
                payload.TryGetProperty("Items", out var itemsElement))
            {
                var orderId = Guid.Parse(orderIdElement.GetString()!);
                var amount = amountElement.GetDecimal();
                var date = storedEvent.OccurredOn.Date;

                // Apply pure projections (no idempotency check, no EventStore write)
                await _repository.UpsertDailyRevenueSummaryAsync(date, amount, 1);
                await _repository.UpsertOrderStateAsync(orderId, "Completed", storedEvent.OccurredOn);

                // Process items if available
                if (itemsElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var itemElement in itemsElement.EnumerateArray())
                    {
                        if (itemElement.TryGetProperty("ProductId", out var productIdElement) &&
                            itemElement.TryGetProperty("ProductName", out var productNameElement) &&
                            itemElement.TryGetProperty("Quantity", out var quantityElement) &&
                            itemElement.TryGetProperty("UnitPrice", out var unitPriceElement))
                        {
                            var productId = Guid.Parse(productIdElement.GetString()!);
                            var productName = productNameElement.GetString()!;
                            var quantity = quantityElement.GetInt32();
                            var unitPrice = unitPriceElement.GetDecimal();
                            var revenue = unitPrice * quantity;

                            await _repository.UpsertTopProductAsync(productId, productName, quantity, revenue, storedEvent.OccurredOn);
                        }
                    }
                }
            }

            _logger.LogDebug("Applied PaymentCompleted event {EventId}", storedEvent.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to apply PaymentCompleted event {EventId}", storedEvent.Id);
            throw;
        }
    }

    private async Task ApplyOrderSubmitted(Dashboard.Domain.Entities.DashboardEventStore storedEvent)
    {
        try
        {
            var payload = JsonSerializer.Deserialize<JsonElement>(storedEvent.Payload);
            
            if (payload.TryGetProperty("OrderId", out var orderIdElement))
            {
                var orderId = Guid.Parse(orderIdElement.GetString()!);
                await _repository.UpsertOrderStateAsync(orderId, "Pending", storedEvent.OccurredOn);
            }

            _logger.LogDebug("Applied OrderSubmitted event {EventId}", storedEvent.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to apply OrderSubmitted event {EventId}", storedEvent.Id);
            throw;
        }
    }

    private async Task ApplyOrderCancelled(Dashboard.Domain.Entities.DashboardEventStore storedEvent)
    {
        try
        {
            var payload = JsonSerializer.Deserialize<JsonElement>(storedEvent.Payload);
            
            if (payload.TryGetProperty("OrderId", out var orderIdElement))
            {
                var orderId = Guid.Parse(orderIdElement.GetString()!);
                await _repository.UpsertOrderStateAsync(orderId, "Cancelled", storedEvent.OccurredOn);
            }

            _logger.LogDebug("Applied OrderCancelled event {EventId}", storedEvent.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to apply OrderCancelled event {EventId}", storedEvent.Id);
            throw;
        }
    }
}