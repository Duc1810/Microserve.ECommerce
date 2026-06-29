using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PaymentService.Models;

namespace PaymentService.Data.Configurations;

public class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.ToTable("transactions");

        // Primary key
        builder.HasKey(t => t.Id);

        // Properties configuration
        builder.Property(t => t.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(t => t.OrderId)
            .HasColumnName("order_id")
            .IsRequired();

        builder.Property(t => t.OrderCode)
            .HasColumnName("order_code")
            .IsRequired();

        builder.Property(t => t.Amount)
            .HasColumnName("amount")
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(t => t.Reference)
            .HasColumnName("reference")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(t => t.Description)
            .HasColumnName("description")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(t => t.IdempotentKey)
            .HasColumnName("idempotent_key")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(t => t.Status)
            .HasColumnName("status")
            .HasConversion<int>()
            .IsRequired();

        builder.Property(t => t.AccountNumber)
            .HasColumnName("account_number")
            .HasMaxLength(50);

        builder.Property(t => t.CounterAccountName)
            .HasColumnName("counter_account_name")
            .HasMaxLength(255);

        builder.Property(t => t.CounterAccountNumber)
            .HasColumnName("counter_account_number")
            .HasMaxLength(50);

        builder.Property(t => t.CounterAccountBankName)
            .HasColumnName("counter_account_bank_name")
            .HasMaxLength(255);

        builder.Property(t => t.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(t => t.UpdatedAt)
            .HasColumnName("updated_at");

        builder.Property(t => t.ProcessedAt)
            .HasColumnName("processed_at");

        // Unique constraints
        builder.HasIndex(t => t.IdempotentKey)
            .IsUnique()
            .HasDatabaseName("ix_transactions_idempotent_key");

        builder.HasIndex(t => t.Reference)
            .IsUnique()
            .HasDatabaseName("ix_transactions_reference");

        // Composite index for performance
        builder.HasIndex(t => new { t.OrderId, t.Status })
            .HasDatabaseName("ix_transactions_order_id_status");

        // Index for querying by status
        builder.HasIndex(t => t.Status)
            .HasDatabaseName("ix_transactions_status");

        // Check constraints
        builder.HasCheckConstraint("ck_transactions_amount_positive", "amount > 0");
        builder.HasCheckConstraint("ck_transactions_status_valid", "status IN (0, 1, 2, 3)");
    }
}