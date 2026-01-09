
using BuildingBlocks.Results;
using System.Security.Cryptography;
using System.Text;


namespace Production.Application.Features.Queries.GetProduct;

public record GetProductsQuery(GetProductsSearchParams Params)
    : IQuery<Result<GetProductsResult>>, ICacheRequest, IVersionedCacheRequest
{
    private const int minuteCacheRedis = 5;
    public TimeSpan? AbsoluteExpirationRelativeToNow => TimeSpan.FromMinutes(minuteCacheRedis);
    private GetProductsSearchParams ProductParam => Params ?? new GetProductsSearchParams();
    public string CacheKey => BuildBaseCacheKey();

    public string BuildBaseCacheKey()
    {
        var page = ProductParam.PageNumber;
        var size = ProductParam.PageSize;
        var name = (ProductParam.NameFilter ?? string.Empty).Trim().ToLowerInvariant();
        var sort = (ProductParam.SortBy ?? string.Empty).Trim();

        var sigRaw = $"page={page}|size={size}|sort={sort}|desc={ProductParam.Descending}|name={name}";
        using var md5 = MD5.Create();
        var hash = Convert.ToHexString(md5.ComputeHash(Encoding.UTF8.GetBytes(sigRaw)));
        return $"product:list:{hash}";
    }

    public IEnumerable<string> VersionScopes()
    {
        yield return "product:list:ver";
        if (!string.IsNullOrWhiteSpace(Params.NameFilter))
            yield return $"product:list:ver:name:{Params.NameFilter.Trim().ToLowerInvariant()}";
    }
}

public record GetProductsResult(PaginatedResult<ProductDto> Page);
