using BuildingBlocks.Observability.BaseEntity;

namespace Dashboard.Domain.Entities;

public class DashboardEventStore : Entity<Guid>
{
    // Id = EventId from IntegrationEvent (inherited from Entity<Guid>)
    public string EventType { get; set; } = string.Empty;   // e.g. "PaymentCompleted"
    public string Payload { get; set; } = string.Empty;     // JSON serialized raw event
    public DateTime OccurredOn { get; set; }                // When the event occurred (UTC) — metadata only
    public DateTime ReceivedAt { get; set; }                // When the consumer received it
    public long SequenceNumber { get; set; }                // Auto-increment, used for ordering during replay
}