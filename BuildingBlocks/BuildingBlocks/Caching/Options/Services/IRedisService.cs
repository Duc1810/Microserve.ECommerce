

using Microsoft.Extensions.Caching.Distributed;

namespace BuildingBlocks.Caching.Services
{
    public interface IRedisService
    {
       

        Task SetAsync<T>(string key, T value, DistributedCacheEntryOptions distributedCacheEntryOptions);

        Task RemoveAsync(string key);

        Task<(bool found, T? value)> TryGetAsync<T>(string key);
    }
}