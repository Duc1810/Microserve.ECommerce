

namespace Order.Application.Features.Queries.GetOrderById;
public class GetOrderQuery
{
    public record GetOrderByIdQuery(Guid OrderId) : IQuery<Result<GetOrderByIdResult>>;

    public record GetOrderByIdResult(DomainOrder Order);
}

