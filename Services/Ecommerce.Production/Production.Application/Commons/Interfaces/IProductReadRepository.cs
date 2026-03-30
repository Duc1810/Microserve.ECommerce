namespace Production.Application.Commons.Interfaces;
public interface IProductRepository
{
    Task<List<Production.Domain.Entities.Product>> GetByCursorAsync(
        DateTime? createdAt,
        Guid? lastId,
        int limit);
}

