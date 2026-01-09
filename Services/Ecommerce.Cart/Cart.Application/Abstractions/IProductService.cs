
namespace Cart.Application.Abstractions;
public interface IProductService
{
    Task<ProductDto?> GetProductAsync(Guid productId, CancellationToken ct = default);

}
