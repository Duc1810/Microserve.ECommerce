using BuildingBlocks.CQRS;
using Dashboard.Application.DTOs;
using Dashboard.Domain.ValueObjects;

namespace Dashboard.Application.Queries;

public record GetTopProductsQuery(int TopN, DateRangeFilter? Filter = null) : IQuery<TopProductsResult>;