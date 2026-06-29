using BuildingBlocks.CQRS;
using Dashboard.Application.DTOs;
using Dashboard.Domain.Enums;
using Dashboard.Domain.ValueObjects;

namespace Dashboard.Application.Queries;

public record GetRevenueTimeSeriesQuery(TimePeriod Period, DateRangeFilter Filter) : IQuery<RevenueTimeSeriesResult>;