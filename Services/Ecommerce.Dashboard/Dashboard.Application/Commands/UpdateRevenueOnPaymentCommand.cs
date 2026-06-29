using BuildingBlocks.CQRS;
using Dashboard.Application.DTOs;

namespace Dashboard.Application.Commands;

public record UpdateRevenueOnPaymentCommand(
    Guid EventId,
    Guid OrderId,
    decimal Amount,
    DateTime OccurredOn,
    List<OrderItemDto> Items
) : ICommand;