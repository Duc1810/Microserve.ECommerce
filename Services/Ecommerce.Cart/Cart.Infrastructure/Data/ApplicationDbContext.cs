using Cart.Domain.Entities;
using Cart.Infrastructure.Data.Configurations;
using Microsoft.EntityFrameworkCore;
namespace Cart.Infrastructure.Data
{
     public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public DbSet<ShoppingCart> Carts { get; set; } = null!;
         public DbSet<CartItem> CartItems { get; set; } = null!;
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfiguration(new CartConfiguration());
            modelBuilder.ApplyConfiguration(new CartItemConfiguration());
        }

    }
}
