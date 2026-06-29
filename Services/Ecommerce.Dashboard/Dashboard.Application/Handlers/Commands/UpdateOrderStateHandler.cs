using BuildingBlocks.CQRS;
using Dashboard.Application.Abstractions;
using Dashboard.Application.Commands;
using Dashboard.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Dashboard.Application.Handlers.Commands;

public class UpdateOrderStateHandler : ICommandHandler<UpdateOrderStateCommand>
{
    private readonly IDashboardDbContext _context;
    private readonly ILogger<UpdateOrderStateHandler> _logger;

    public UpdateOrderStateHandler(
        IDashboardDbContext context,
        ILogger<UpdateOrderStateHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Unit> Handle(UpdateOrderStateCommand request, CancellationToken cancellationToken)
    {
        var existingOrderState = await _context.OrderStateRecords
            .FirstOrDefaultAsync(x => x.Id == request.OrderId, cancellationToken);

        if (existingOrderState != null)
        {
            existingOrderState.Status = request.NewStatus;
            existingOrderState.LastUpdatedAt = request.OccurredOn;
        }
        else
        {
            var newOrderState = new OrderStateRecord
            {
                Id = request.OrderId,
                Status = request.NewStatus,
                LastUpdatedAt = request.OccurredOn
            };
            _context.OrderStateRecords.Add(newOrderState);
        }

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Updated order {OrderId} status to {Status}", 
            request.OrderId, request.NewStatus);

        return Unit.Value;
    }
}