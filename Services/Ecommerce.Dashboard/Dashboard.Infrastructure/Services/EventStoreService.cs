using Dashboard.Application.Abstractions;
using Dashboard.Domain.Entities;
using Microsoft.Extensions.Logging;
using Dapper;
using System.Data;
using Npgsql;
using Microsoft.Extensions.Configuration;

namespace Dashboard.Infrastructure.Services;

/// <summary>
/// Service for handling EventStore operations including atomic idempotency checks using Dapper
/// </summary>
public class EventStoreService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EventStoreService> _logger;

    public EventStoreService(IConfiguration configuration, ILogger<EventStoreService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Atomically inserts an event into the EventStore using INSERT ON CONFLICT DO NOTHING pattern
    /// Returns true if the event was inserted (new), false if it already existed (duplicate)
    /// </summary>
    public async Task<bool> TryInsertEventAsync(DashboardEventStore eventStore, CancellationToken cancellationToken = default)
    {
        try
        {
            using var connection = new NpgsqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            await connection.OpenAsync(cancellationToken);

            const string sql = @"
                INSERT INTO dashboard_event_store (id, event_type, payload, occurred_on, received_at) 
                VALUES (@Id, @EventType, @Payload, @OccurredOn, @ReceivedAt) 
                ON CONFLICT (id) DO NOTHING";

            var rowsAffected = await connection.ExecuteAsync(sql, new
            {
                Id = eventStore.Id,
                EventType = eventStore.EventType,
                Payload = eventStore.Payload,
                OccurredOn = eventStore.OccurredOn,
                ReceivedAt = eventStore.ReceivedAt
            });

            var isNewEvent = rowsAffected > 0;
            
            if (!isNewEvent)
            {
                _logger.LogInformation("Duplicate event detected: {EventId}, skipping processing", eventStore.Id);
            }

            return isNewEvent;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inserting event {EventId} into EventStore", eventStore.Id);
            throw;
        }
    }

    /// <summary>
    /// Gets all events from EventStore ordered by sequence number for rebuild operations
    /// </summary>
    public async Task<IEnumerable<DashboardEventStore>> GetAllEventsOrderedAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var connection = new NpgsqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            await connection.OpenAsync(cancellationToken);

            const string sql = @"
                SELECT id, event_type, payload, occurred_on, received_at, sequence_number 
                FROM dashboard_event_store 
                ORDER BY sequence_number ASC";

            var events = await connection.QueryAsync<DashboardEventStore>(sql);
            return events;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving events from EventStore");
            throw;
        }
    }
}