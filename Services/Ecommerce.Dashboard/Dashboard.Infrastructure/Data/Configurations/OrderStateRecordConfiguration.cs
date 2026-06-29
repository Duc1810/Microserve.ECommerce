using Dashboard.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dashboard.Infrastructure.Data.Configurations;

public class OrderStateRecordConfiguration : IEntityTypeConfiguration<OrderStateRecord>
{
    public void Configure(EntityTypeBuilder<OrderStateRecord> builder)
    {
        builder.ToTable("order_state_records");

        // Id is OrderId - primary key
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName("order_id");

        builder.Property(x => x.Status)
            .IsRequired()
            .HasMaxLength(20)
            .HasColumnName("status");

        builder.Property(x => x.LastUpdatedAt)
            .IsRequired()
            .HasColumnName("last_updated_at");

        // Check constraint for valid status values
        builder.HasCheckConstraint("ck_order_state_records_status_valid", 
            "status IN ('Draft', 'Pending', 'Completed', 'Cancelled')");
    }
}