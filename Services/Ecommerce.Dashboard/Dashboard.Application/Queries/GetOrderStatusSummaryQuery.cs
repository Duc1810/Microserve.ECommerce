using BuildingBlocks.CQRS;
using Dashboard.Application.DTOs;

namespace Dashboard.Application.Queries;

public record GetOrderStatusSummaryQuery() : IQuery<OrderStatusSummaryResult>;