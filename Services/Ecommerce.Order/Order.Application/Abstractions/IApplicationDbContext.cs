
namespace Order.Application.Abstractions
{
    public interface IApplicationDbContext
    {
        DbSet<Order.Domain.Models.Customer> Customers { get; }
        DbSet<Order.Domain.Models.Order> Orders { get; }
        DbSet<OrderItem> OrderItems { get; }

        Task<int> SaveChangesAsync(CancellationToken cancellationToken);
    }
}
