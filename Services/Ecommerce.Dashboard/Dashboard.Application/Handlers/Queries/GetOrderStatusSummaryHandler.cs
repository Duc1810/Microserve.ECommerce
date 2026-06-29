using BuildingBlocks.CQRS;
using Dashboard.Application.Abstractions;
using Dashboard.Application.DTOs;
using Dashboard.Application.Queries;

namespace Dashboard.Application.Handlers.Queries;

public class GetOrderStatusSummaryHandler : IQueryHandler<GetOrderStatusSummaryQuery, OrderStatusSummaryResult>
{
    private readonly IDashboardRepository _repository;

    public GetOrderStatusSummaryHandler(IDashboardRepository repository)
    {
        _repository = repository;
    }

    public async Task<OrderStatusSummaryResult> Handle(GetOrderStatusSummaryQuery request, CancellationToken cancellationToken)
    {
        var results = await _repository.GetOrderStatusSummaryAsync();
        return results.First();
    }
}