using Grpc.Core;
using MediatR;
using Production.Application.Features.Queries.GetProductById;
using Production.Domain.Entities;
using Production.Grpc;

namespace Production.API.Services
{
    public class ProductServiceImpl : ProductService.ProductServiceBase
    {
        private readonly IMediator _mediator;

        public ProductServiceImpl(IMediator mediator)
        {
            _mediator = mediator;
        }

        public override async Task<GetProductResponse> GetProduct(GetProductRequest request, ServerCallContext context)
        {
            var query = new GetProductByIdQuery(Guid.Parse(request.Id));
            
            var result = await _mediator.Send(query);
            var product = result.Value?.Product;

            if (product is null)
            {
                throw new RpcException(new Status(StatusCode.NotFound, $"Product {request.Id} not found"));
            }

            var response = new GetProductResponse
            {
                Id = product.Id.ToString(),
                Name = product.Name,
                Description = product.Description,
                ImageFile = product.ImageFile,
                Price = (double)product.Price,
                Quantity = product.Quantity,
                
            };

            response.Category.AddRange(product.Category);

            return response;
        }
    }
}
