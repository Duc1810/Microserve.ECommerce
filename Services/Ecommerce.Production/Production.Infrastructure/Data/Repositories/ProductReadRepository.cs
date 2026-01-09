//using Microsoft.EntityFrameworkCore;
//using product.Infrastructure.Data;
//using Production.Application.Abstractions;
//using Production.Application.Abstractions.Pagination;
//using Production.Domain.Entities;
//using System.Linq.Expressions;

//public class ProductReadRepository(ApplicationDbContext db) : IProductReadRepository
//{
//    public async Task<CursorPage<T>> GetByCursorAsync<T>(
//        CursorRequest req,
//        Expression<Func<Product, bool>>? filter,
//        Expression<Func<Product, T>> selector,
//        bool descending = true,
//        CancellationToken ct = default)
//    {
//        IQueryable<Product> query = db.Set<Product>().AsNoTracking();

//        if (filter is not null) query = query.Where(filter);

//        var decoded = CursorCodec.Decode(req.Cursor);


//        if (decoded is not null)
//        {
//            if (descending)
//            {

//                query = query.Where(x =>
//                    EF.Functions.LessThan(
//                        ValueTuple.Create(x.CreatedAt, x.Id),
//                        ValueTuple.Create(decoded.Date, decoded.LastId)
//                    ));
//            }
//            else
//            {

//                query = query.Where(x =>
//                    EF.Functions.GreaterThan(
//                        ValueTuple.Create(x.CreatedAt, x.Id),
//                        ValueTuple.Create(decoded.Date, decoded.LastId)
//                    ));
//            }
//        }


//        var ordered = descending
//            ? query.OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id)
//            : query.OrderBy(x => x.CreatedAt).ThenBy(x => x.Id);

//        var raw = await ordered.Select(selector).Take(req.PageSize + 1).ToListAsync(ct);
//        var hasMore = raw.Count > req.PageSize;
//        if (hasMore) raw.RemoveAt(raw.Count - 1);

//        return new CursorPage<T>(raw, hasMore);
//    }
//}
