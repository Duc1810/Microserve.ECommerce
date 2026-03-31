using Microsoft.EntityFrameworkCore;
using product.Infrastructure.Data;
using Production.Application.Commons.Interfaces;

public class ProductRepository : IProductRepository
{
    private readonly ApplicationDbContext _context;

    public ProductRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<Production.Domain.Entities.Product>> GetByCursorAsync(DateTime? createdAt, Guid? lastId, int limit)
    {
        var query = _context.Products.AsNoTracking().Where(p => !p.IsDeleted);

        if(createdAt.HasValue && lastId.HasValue)
        {
            //query = query.Where(p => p.CreatedAt < createdAt.Value || (p.CreatedAt == createdAt.Value && p.Id < lastId.Value));
            query = query.Where(x => EF.Functions.LessThanOrEqual(
                ValueTuple.Create(x.CreatedAt, x.Id),
                ValueTuple.Create(createdAt, lastId)
                ));
        }

        return await query
            .OrderByDescending(x => x.CreatedAt)
            .ThenByDescending(x => x.Id)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<IEnumerable<(string Category, int Count, decimal AvgPrice, decimal MinPrice, decimal MaxPrice)>> GetCategoriesStatsAsync(
        CancellationToken cancellationToken = default)
    {
        var stats = await _context.Products
            .AsNoTracking()
            .Where(product => !product.IsDeleted)
            .SelectMany(product => product.Category.Select(category => new { Category = category, product.Price }))
            .Where(x => !string.IsNullOrWhiteSpace(x.Category))
            .GroupBy(x => x.Category.Trim())
            .Select(group => new
            {
                Category = group.Key,
                Count = group.Count(),
                AvgPrice = decimal.Round(group.Average(x => x.Price), 2),
                MinPrice = group.Min(x => x.Price),
                MaxPrice = group.Max(x => x.Price)
            })
            .ToListAsync(cancellationToken);

        return stats.Select(s => (s.Category, s.Count, s.AvgPrice, s.MinPrice, s.MaxPrice));
    }
}
