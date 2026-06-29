using BuildingBlocks.CQRS;
using Dashboard.Application.Abstractions;
using Dashboard.Application.DTOs;
using Dashboard.Application.Queries;
using Microsoft.EntityFrameworkCore;

namespace Dashboard.Application.Handlers.Queries;

public class GetTopProductsHandler : IQueryHandler<GetTopProductsQuery, TopProductsResult>
{
    private readonly IDashboardDbContext _context;

    public GetTopProductsHandler(IDashboardDbContext context)
    {
        _context = context;
    }

    public async Task<TopProductsResult> Handle(GetTopProductsQuery request, CancellationToken cancellationToken)
    {
        var query = _context.TopProductSnapshots.AsQueryable();

        // Apply date filter if provided
        if (request.Filter != null)
        {
            query = query.Where(x => x.LastSoldAt >= request.Filter.FromDate && x.LastSoldAt <= request.Filter.ToDate);
        }

        var topProducts = await query
            .OrderByDescending(x => x.TotalQuantitySold)
            .Take(request.TopN)
            .Select(x => new TopProductItem(
                x.ProductId,
                x.ProductName,
                x.TotalQuantitySold,
                x.TotalRevenue
            ))
            .ToListAsync(cancellationToken);

        return new TopProductsResult(topProducts);
    }
}