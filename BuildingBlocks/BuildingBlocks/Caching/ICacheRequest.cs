

namespace BuildingBlocks.Caching
{
    public interface ICacheRequest
    {
        string CacheKey { get; }
        TimeSpan? AbsoluteExpirationRelativeToNow { get; }
    }
}
