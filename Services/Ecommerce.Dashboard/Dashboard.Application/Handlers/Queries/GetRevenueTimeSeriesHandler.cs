using BuildingBlocks.CQRS;
using Dashboard.Application.Abstractions;
using Dashboard.Application.DTOs;
using Dashboard.Application.Queries;
using Dashboard.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Dashboard.Application.Handlers.Queries;

public class GetRevenueTimeSeriesHandler : IQueryHandler<GetRevenueTimeSeriesQuery, RevenueTimeSeriesResult>
{
    private readonly IDashboardDbContext _context;

    public GetRevenueTimeSeriesHandler(IDashboardDbContext context)
    {
        _context = context;
    }

    public async Task<RevenueTimeSeriesResult> Handle(GetRevenueTimeSeriesQuery request, CancellationToken cancellationToken)
    {
        var dailyData = await _context.DailyRevenueSummaries
            .Where(x => x.Date >= request.Filter.FromDate && x.Date <= request.Filter.ToDate)
            .OrderBy(x => x.Date)
            .ToListAsync(cancellationToken);

        var dataPoints = request.Period switch
        {
            TimePeriod.Daily => dailyData.Select(d => new RevenueDataPoint(
                d.Date,
                d.TotalRevenue,
                d.CompletedOrderCount
            )).ToList(),

            TimePeriod.Weekly => dailyData
                .GroupBy(d => GetWeekStart(d.Date))
                .Select(g => new RevenueDataPoint(
                    g.Key,
                    g.Sum(d => d.TotalRevenue),
                    g.Sum(d => d.CompletedOrderCount)
                ))
                .OrderBy(d => d.Date)
                .ToList(),

            TimePeriod.Monthly => dailyData
                .GroupBy(d => new DateTime(d.Date.Year, d.Date.Month, 1))
                .Select(g => new RevenueDataPoint(
                    g.Key,
                    g.Sum(d => d.TotalRevenue),
                    g.Sum(d => d.CompletedOrderCount)
                ))
                .OrderBy(d => d.Date)
                .ToList(),

            _ => throw new ArgumentException($"Unsupported period: {request.Period}")
        };

        return new RevenueTimeSeriesResult(request.Period, dataPoints);
    }

    private static DateTime GetWeekStart(DateTime date)
    {
        var diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
        return date.AddDays(-1 * diff).Date;
    }
}