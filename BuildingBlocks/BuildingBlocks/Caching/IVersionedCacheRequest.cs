

namespace BuildingBlocks.Caching
{
    public interface IVersionedCacheRequest
    {
        string BuildBaseCacheKey();
        IEnumerable<string> VersionScopes();
    }
}
