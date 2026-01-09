
namespace Production.Application.Features.Queries.GetProductById;

public record GetProductByIdQuery(Guid Id) : IQuery<Result<GetProductsByIdResult>>;

public record GetProductsByIdResult(Production.Domain.Entities.Product Product);
