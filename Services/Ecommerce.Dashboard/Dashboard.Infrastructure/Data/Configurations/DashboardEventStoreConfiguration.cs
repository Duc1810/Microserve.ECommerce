using Dashboard.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dashboard.Infrastructure.Data.Configurations;

public class DashboardEventStoreConfiguration : IEntityTypeConfiguration<DashboardEventStore>
{
    public void Configure(EntityTypeBuilder<DashboardEventStore> builder)
    {
        builder.ToTable("dashboard_event_store");

        // Id is EventId from integration event - primary key
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName("event_id");

        builder.Property(x => x.EventType)
            .IsRequired()
            .HasMaxLength(100)
            .HasColumnName("event_type");

        builder.Property(x => x.Payload)
            .IsRequired()
            .HasColumnType("jsonb") // PostgreSQL JSONB for better performance
            .HasColumnName("payload");

        builder.Property(x => x.OccurredOn)
            .IsRequired()
            .HasColumnName("occurred_on");

        builder.Property(x => x.ReceivedAt)
            .IsRequired()
            .HasColumnName("received_at");

        builder.Property(x => x.SequenceNumber)
            .IsRequired()
            .HasColumnName("sequence_number")
            .ValueGeneratedOnAdd(); // Auto-increment (BIGSERIAL in PostgreSQL)

        // Index on SequenceNumber for ordering during replay
        builder.HasIndex(x => x.SequenceNumber)
            .HasDatabaseName("ix_dashboard_event_store_sequence_number");

        // Index on EventType for filtering
        builder.HasIndex(x => x.EventType)
            .HasDatabaseName("ix_dashboard_event_store_event_type");

        // Index on ReceivedAt for time-based queries
        builder.HasIndex(x => x.ReceivedAt)
            .HasDatabaseName("ix_dashboard_event_store_received_at");
    }
}