using Cart.Application.Abstractions;
using Cart.Application.Dtos;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Proto = Cart.GrpcContracts;

namespace Cart.Infrastructure.Grpc.Clients;

public sealed class ProductGrpcClient : IProductService
{
    private readonly Proto.ProductService.ProductServiceClient _client;
    private readonly ILogger<ProductGrpcClient> _logger;

    public ProductGrpcClient(
        Proto.ProductService.ProductServiceClient client,
        ILogger<ProductGrpcClient> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task<ProductDto?> GetProductAsync(Guid productId, CancellationToken ct = default)
    {
        const int timeoutSeconds = 5;

        try
        {
            var res = await _client.GetProductAsync(
                new Proto.GetProductRequest { Id = productId.ToString() },
                new CallOptions(deadline: DateTime.UtcNow.AddSeconds(timeoutSeconds), cancellationToken: ct));

            if (res is null)
            {
                return null;
            }

            var mappedId = Guid.TryParse(res.Id, out var id) ? id : Guid.Empty;

            return new ProductDto(
                Id: mappedId,
                Name: res.Name,
                Description: res.Description,
                Price: Convert.ToDecimal(res.Price),
                Quantity: (int)res.Quantity,
                Categories: res.Category?.ToList() ?? new List<string>(),
                ImageFile: res.ImageFile ?? string.Empty
            );
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
        {
            _logger.LogWarning(ex, "[gRPC] Product not found: {ProductId}", productId);
            return null;
        }
        catch (RpcException ex) when (ex.StatusCode is StatusCode.Unavailable or StatusCode.DeadlineExceeded)
        {
            _logger.LogError(ex, "[gRPC] Product service unavailable/deadline exceeded.");
            return null;
        }
    }
}

