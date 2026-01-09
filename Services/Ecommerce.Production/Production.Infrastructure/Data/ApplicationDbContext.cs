using Microsoft.EntityFrameworkCore;
using Production.Infrastructure.Data.Configurations;

namespace product.Infrastructure.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

    public DbSet<Production.Domain.Entities.Product> Products { get; set; } = null!;
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfiguration(new ProductConfiguration());

        modelBuilder.Entity<Production.Domain.Entities.Product>()
            .HasIndex(p => new { p.CreatedAt, p.Id })
                .HasDatabaseName("IX_Product_Date_Id");

    }

}