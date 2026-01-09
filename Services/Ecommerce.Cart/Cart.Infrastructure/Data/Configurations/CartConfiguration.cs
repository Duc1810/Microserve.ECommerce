using Cart.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cart.Infrastructure.Data.Configurations;
public class CartConfiguration : IEntityTypeConfiguration<ShoppingCart>
{
    public void Configure(EntityTypeBuilder<ShoppingCart> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.UserName)
               .IsRequired()
               .HasMaxLength(100);

        builder.HasIndex(c => c.UserName)
               .IsUnique();

        builder.HasMany(c => c.CartItems)
               .WithOne(ci => ci.Cart)
               .HasForeignKey(ci => ci.CartId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.ToTable("Carts");
    }
}



