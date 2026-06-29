using BuildingBlocks.CQRS;
using Dashboard.Application.DTOs;
using Dashboard.Domain.ValueObjects;

namespace Dashboard.Application.Queries;

public record GetRevenueSummaryQuery(DateRangeFilter Filter) : IQuery<RevenueSummaryResult>;