
using BuildingBlocks.Observability.Pagination;

namespace Order.Application.UserCases.GetOrder;

public record GetOrdersQuery(GetOrdersSearchParams Params) : IQuery<Result<GetOrdersResult>>;


public record GetOrdersResult(PaginatedResult<OrderDto> Lists);
