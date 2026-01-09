using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Production.Infrastructure.Data.Configurations
{
    internal class ProductConfiguration : IEntityTypeConfiguration<Production.Domain.Entities.Product>
    {
        public void Configure(EntityTypeBuilder<Production.Domain.Entities.Product> builder)
        {

            builder.ToTable("Products");


            builder.HasKey(p => p.Id);


            builder.Property(p => p.Name)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(p => p.Description)
                .HasMaxLength(1000);

            builder.Property(p => p.ImageFile)
                .HasMaxLength(500);

            builder.Property(p => p.Price)
                .HasColumnType("decimal(18,2)");

            builder.Property(p => p.Quantity)
                .HasColumnType("int");



        }
    }
}
