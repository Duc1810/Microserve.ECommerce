namespace Dashboard.Application.DTOs;

public record OrderStatusSummaryResult(
    int Draft,
    int Pending,
    int Completed,
    int Cancelled,
    int Total
);