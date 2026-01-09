using BuildingBlocks.Caching.Options;
using Caching.Helpers;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using StackExchange.Redis;



namespace BuildingBlocks.Caching.Services
{
    public class RedisService : IRedisService
    {
        private readonly IDistributedCache _distributedCache;
        private readonly IDatabase Database;
        private readonly string _prefix;

        private const int RedisDefaultSlidingExpirationInSecond = 3600;

        public RedisService(IDistributedCache distributedCache, IConnectionMultiplexer connectionMultiplexer, IOptions<RedisOptions> options)
        {
            Database = connectionMultiplexer.GetDatabase();
            _distributedCache = distributedCache;
            _prefix = (options?.Value?.Prefix ?? "app:").Trim();
            if (!string.IsNullOrEmpty(_prefix) && !_prefix.EndsWith(":"))
            {
                _prefix += ":";
            }
        }




        public async Task SetAsync<T>(string key, T value, DistributedCacheEntryOptions distributedCacheEntryOptions)
        {
            await _distributedCache.SetStringAsync(_prefix + key, CacheHelper.Serialize(value), distributedCacheEntryOptions);
        }
        public async Task RemoveAsync(string key)
        {
            await _distributedCache.RemoveAsync(_prefix + key);
        }





        public async Task<(bool found, T? value)> TryGetAsync<T>(string key)
        {
            var valueAsString = await _distributedCache.GetStringAsync(_prefix + key);
            if (string.IsNullOrWhiteSpace(valueAsString))
                return (false, default);
            return (true, CacheHelper.Deserialize<T>(valueAsString));
        }
    }
}
