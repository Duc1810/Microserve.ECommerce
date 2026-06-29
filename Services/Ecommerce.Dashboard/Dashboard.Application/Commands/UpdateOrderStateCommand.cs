using BuildingBlocks.CQRS;

namespace Dashboard.Application.Commands;

public record UpdateOrderStateCommand(
    Guid OrderId,
    string NewStatus,
    DateTime OccurredOn
) : ICommand;