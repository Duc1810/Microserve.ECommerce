using BuildingBlocks.CQRS;
using Dashboard.Application.Abstractions;
using Dashboard.Application.DTOs;
using Dashboard.Application.Queries;

namespace Dashboard.Application.Handlers.Queries;

public class GetRevenueSummaryHandler : IQueryHandler<GetRevenueSummaryQuery, RevenueSummaryResult>
{
    private readonly IDashboardRepository _repository;

    public GetRevenueSummaryHandler(IDashboardRepository repository)
    {
        _repository = repository;
    }

    public async Task<RevenueSummaryResult> Handle(GetRevenueSummaryQuery request, CancellationToken cancellationToken)
    {
        var summaries = await _repository.GetRevenueSummaryAsync(
            request.Filter.FromDate, 
            request.Filter.ToDate);

        var totalRevenue = summaries.Sum(x => x.TotalRevenue);
        var totalCompletedOrders = summaries.Sum(x => x.CompletedOrderCount);
        var averageOrderValue = totalCompletedOrders > 0 ? totalRevenue / totalCompletedOrders : 0;

        return new RevenueSummaryResult(
            totalRevenue,
            totalCompletedOrders,
            averageOrderValue,
            request.Filter.FromDate,
            request.Filter.ToDate
        );
    }
}