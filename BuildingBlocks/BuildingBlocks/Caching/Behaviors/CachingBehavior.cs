using BuildingBlocks.Caching;
using BuildingBlocks.Caching.Services;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;


namespace Caching.Behaviors
{

    public class CachingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
        where TResponse : notnull
    {
        private readonly ILogger<CachingBehavior<TRequest, TResponse>> _logger;
        private readonly IRedisService _redisService;
        private readonly IVersionStore _versionStore;

        private const int DefaultCacheExpirationInHours = 1;

        public CachingBehavior(
            ILogger<CachingBehavior<TRequest, TResponse>> logger,
            IRedisService redisService,
            IVersionStore versionStore)
        {
            _logger = logger;
            _redisService = redisService;
            _versionStore = versionStore;
        }

        public async Task<TResponse> Handle(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken)
        {

            if (request is not ICacheRequest cacheRequest)
                return await next();


            var cacheKey = await BuildCacheKeyAsync(request, cacheRequest, cancellationToken);


            var (found, cached) = await _redisService.TryGetAsync<TResponse>(cacheKey);
            if (found && !Equals(cached, default(TResponse)))
            {
                _logger.LogDebug("Response retrieved {TRequest} from cache. CacheKey: {CacheKey}",
                    typeof(TRequest).FullName, cacheKey);
                return cached!;
            }

            var response = await next();

            var ttl = cacheRequest.AbsoluteExpirationRelativeToNow
                      ?? TimeSpan.FromHours(DefaultCacheExpirationInHours);

            await _redisService.SetAsync(cacheKey, response, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = ttl
            });

            _logger.LogDebug("Caching response for {TRequest} with cache key: {CacheKey}",
                typeof(TRequest).FullName, cacheKey);

            return response;
        }

        private async Task<string> BuildCacheKeyAsync(
            TRequest request,
            ICacheRequest cacheRequest,
            CancellationToken ct)
        {

            if (request is IVersionedCacheRequest vreq)
            {
                var baseKey = vreq.BuildBaseCacheKey();
                var scopes = vreq.VersionScopes() ?? Enumerable.Empty<string>();

                var versionParts = new List<string>();
                foreach (var scope in scopes)
                {
                    var v = await _versionStore.GetAsync(scope, ct);
                    var scopeToken = SanitizeScope(scope);
                    versionParts.Add($"{scopeToken}-v{v}");
                }

                var verPart = versionParts.Count > 0 ? string.Join(":", versionParts) : "v0";
                return $"{baseKey}:{verPart}";
            }

            return cacheRequest.CacheKey;
        }

        private static string SanitizeScope(string scope)
            => string.IsNullOrWhiteSpace(scope) ? "scope" : scope.Replace(':', '_');
    }
}
