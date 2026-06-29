using Dashboard.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dashboard.Infrastructure.Data.Configurations;

public class DailyRevenueSummaryConfiguration : IEntityTypeConfiguration<DailyRevenueSummary>
{
    public void Configure(EntityTypeBuilder<DailyRevenueSummary> builder)
    {
        builder.ToTable("daily_revenue_summaries");

        builder.HasKey(x => x.Id);

        // Unique index on Date - one record per day
        builder.HasIndex(x => x.Date)
            .IsUnique()
            .HasDatabaseName("ix_daily_revenue_summaries_date");

        builder.Property(x => x.Date)
            .IsRequired()
            .HasColumnName("date");

        builder.Property(x => x.TotalRevenue)
            .IsRequired()
            .HasColumnType("decimal(18,2)")
            .HasColumnName("total_revenue");

        builder.Property(x => x.CompletedOrderCount)
            .IsRequired()
            .HasColumnName("completed_order_count");

        builder.Property(x => x.CancelledOrderCount)
            .IsRequired()
            .HasColumnName("cancelled_order_count");

        // Ensure TotalRevenue >= 0
        builder.HasCheckConstraint("ck_daily_revenue_summaries_total_revenue_non_negative", 
            "total_revenue >= 0");
    }
}