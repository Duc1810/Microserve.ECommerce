namespace Dashboard.Application.DTOs;

public record RevenueSummaryResult(
    decimal TotalRevenue,
    int TotalCompletedOrders,
    decimal AverageOrderValue,
    DateTime FromDate,
    DateTime ToDate
);