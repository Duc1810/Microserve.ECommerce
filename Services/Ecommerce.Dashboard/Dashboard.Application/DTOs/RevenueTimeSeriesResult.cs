using Dashboard.Domain.Enums;

namespace Dashboard.Application.DTOs;

public record RevenueTimeSeriesResult(
    TimePeriod Period,
    List<RevenueDataPoint> DataPoints
);

public record RevenueDataPoint(
    DateTime Date,
    decimal Revenue,
    int OrderCount
);