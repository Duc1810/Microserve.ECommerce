using Cart.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cart.Infrastructure.Data.Configurations;
public class CartItemConfiguration : IEntityTypeConfiguration<CartItem>
{
    public void Configure(EntityTypeBuilder<CartItem> builder)
    {
        builder.HasKey(ci => ci.Id);

        builder.Property(ci => ci.ProductName)
               .IsRequired()
               .HasMaxLength(200);

        builder.Property(ci => ci.Color)
               .IsRequired()
               .HasMaxLength(50);

        builder.Property(ci => ci.Price)
               .IsRequired()
               .HasColumnType("decimal(18,2)");

        builder.Property(ci => ci.Quantity)
               .IsRequired();

        builder.ToTable("CartItems");
    }
}