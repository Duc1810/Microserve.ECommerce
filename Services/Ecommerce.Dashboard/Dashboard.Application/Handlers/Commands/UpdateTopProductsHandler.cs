using BuildingBlocks.CQRS;
using Dashboard.Application.Abstractions;
using Dashboard.Application.Commands;
using Dashboard.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Dashboard.Application.Handlers.Commands;

public class UpdateTopProductsHandler : ICommandHandler<UpdateTopProductsCommand>
{
    private readonly IDashboardDbContext _context;
    private readonly ILogger<UpdateTopProductsHandler> _logger;

    public UpdateTopProductsHandler(
        IDashboardDbContext context,
        ILogger<UpdateTopProductsHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Unit> Handle(UpdateTopProductsCommand request, CancellationToken cancellationToken)
    {
        foreach (var item in request.Items)
        {
            var existingProduct = await _context.TopProductSnapshots
                .FirstOrDefaultAsync(x => x.ProductId == item.ProductId, cancellationToken);

            if (existingProduct != null)
            {
                existingProduct.TotalQuantitySold += item.Quantity;
                existingProduct.TotalRevenue += item.UnitPrice * item.Quantity;
                existingProduct.LastSoldAt = request.OccurredOn;
                existingProduct.ProductName = item.ProductName; // Update name in case it changed
            }
            else
            {
                var newProduct = new TopProductSnapshot
                {
                    Id = Guid.NewGuid(),
                    ProductId = item.ProductId,
                    ProductName = item.ProductName,
                    TotalQuantitySold = item.Quantity,
                    TotalRevenue = item.UnitPrice * item.Quantity,
                    LastSoldAt = request.OccurredOn
                };
                _context.TopProductSnapshots.Add(newProduct);
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Updated top products data for {ItemCount} items", request.Items.Count);

        return Unit.Value;
    }
}