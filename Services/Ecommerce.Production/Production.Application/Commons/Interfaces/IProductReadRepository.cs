namespace Production.Application.Commons.Interfaces;
public interface IProductRepository
{
    Task<List<Production.Domain.Entities.Product>> GetByCursorAsync(
        DateTime? createdAt,
        Guid? lastId,
        int limit);

    Task<IEnumerable<(string Category, int Count, decimal AvgPrice, decimal MinPrice, decimal MaxPrice)>> GetCategoriesStatsAsync(
        CancellationToken cancellationToken = default);
}
