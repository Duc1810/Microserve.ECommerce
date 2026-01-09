using BuildingBlocks.Caching.Options;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace BuildingBlocks.Caching.Services;

public class RedisVersionStore : IVersionStore
{
    private readonly IConnectionMultiplexer _mux;
    private readonly string _prefix;

    public RedisVersionStore(IConnectionMultiplexer mux, IOptions<RedisOptions> opt)
    {
        _mux = mux;
        _prefix = string.IsNullOrWhiteSpace(opt.Value.Prefix) ? "app:" :
                  (opt.Value.Prefix.EndsWith(":") ? opt.Value.Prefix : opt.Value.Prefix + ":");
    }

    public async Task<long> GetAsync(string key, CancellationToken ct = default)
    {
        var db = _mux.GetDatabase();
        var v = await db.StringGetAsync(_prefix + key);
        return v.HasValue ? (long)v : 0;
    }

    public async Task<long> BumpAsync(string key, CancellationToken ct = default)
    {
        var db = _mux.GetDatabase();
        return await db.StringIncrementAsync(_prefix + key);
    }
}
