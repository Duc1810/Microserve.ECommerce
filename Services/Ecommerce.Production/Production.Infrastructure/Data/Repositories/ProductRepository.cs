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
}
