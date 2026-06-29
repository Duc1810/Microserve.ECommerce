using Dashboard.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dashboard.Infrastructure.Data.Configurations;

public class TopProductSnapshotConfiguration : IEntityTypeConfiguration<TopProductSnapshot>
{
    public void Configure(EntityTypeBuilder<TopProductSnapshot> builder)
    {
        builder.ToTable("top_product_snapshots");

        builder.HasKey(x => x.Id);

        // Unique index on ProductId
        builder.HasIndex(x => x.ProductId)
            .IsUnique()
            .HasDatabaseName("ix_top_product_snapshots_product_id");

        builder.Property(x => x.ProductId)
            .IsRequired()
            .HasColumnName("product_id");

        builder.Property(x => x.ProductName)
            .IsRequired()
            .HasMaxLength(255)
            .HasColumnName("product_name");

        builder.Property(x => x.TotalQuantitySold)
            .IsRequired()
            .HasColumnName("total_quantity_sold");

        builder.Property(x => x.TotalRevenue)
            .IsRequired()
            .HasColumnType("decimal(18,2)")
            .HasColumnName("total_revenue");

        builder.Property(x => x.LastSoldAt)
            .IsRequired()
            .HasColumnName("last_sold_at");

        // Check constraints for non-negative values
        builder.HasCheckConstraint("ck_top_product_snapshots_quantity_non_negative", 
            "total_quantity_sold >= 0");
        
        builder.HasCheckConstraint("ck_top_product_snapshots_revenue_non_negative", 
            "total_revenue >= 0");
    }
}