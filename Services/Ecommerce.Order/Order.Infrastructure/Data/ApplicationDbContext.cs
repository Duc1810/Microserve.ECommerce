using Microsoft.EntityFrameworkCore;
using System.Reflection;
using Order.Domain.Models;
using Order.Application.Abstractions;
using MassTransit;
using Order.Application.Sagas;
namespace Order.Infrastructure.Data
{
    public class ApplicationDbContext : DbContext, IApplicationDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public DbSet<Customer> Customers => Set<Customer>();
        public DbSet<Domain.Models.Order> Orders => Set<Domain.Models.Order>();
        public DbSet<OrderItem> OrderItems => Set<OrderItem>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

            builder.AddInboxStateEntity();
            builder.AddOutboxMessageEntity();
            builder.AddOutboxStateEntity();

            //saga state
            builder.Entity<OrderState>().HasKey(x => x.CorrelationId);
        }
    }
}
