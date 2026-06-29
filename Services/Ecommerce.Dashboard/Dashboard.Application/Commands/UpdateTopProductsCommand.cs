using BuildingBlocks.CQRS;
using Dashboard.Application.DTOs;

namespace Dashboard.Application.Commands;

public record UpdateTopProductsCommand(
    List<OrderItemDto> Items,
    DateTime OccurredOn
) : ICommand;