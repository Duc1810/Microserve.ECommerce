

namespace Production.Application.Features.Queries.GetProductCursor;
public class GetProductsCursorQuery
    : IQuery<Result<GetProductsCursorResult>>
{
    public ProductCursorParams Params { get; set; }
}
