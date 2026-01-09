using BuildingBlocks.Caching;
using BuildingBlocks.Caching.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Caching.Behaviors
{
    /// <summary>
    /// Invalidate cho MediatR sau khi handler chạy xong.
    /// - Nếu request implement IVersionBumpRequest: bump (INCR) các version scope -> generational invalidation.
    /// - Nếu request implement IInvalidateCacheRequest: xóa đúng 1 cache key (giữ tương thích với code cũ).
    /// </summary>
    public class InvalidateCachingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
        where TResponse : notnull
    {
        private readonly ILogger<InvalidateCachingBehavior<TRequest, TResponse>> _logger;
        private readonly IRedisService _redisService;
        private readonly IVersionStore _versionStore;

        public InvalidateCachingBehavior(
            ILogger<InvalidateCachingBehavior<TRequest, TResponse>> logger,
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
            var response = await next();

            if (request is IVersionBumpRequest bumpReq)
            {
                var scopes = bumpReq.VersionScopesToBump() ?? Enumerable.Empty<string>();
                foreach (var scope in scopes)
                {
                    await _versionStore.BumpAsync(scope, cancellationToken);
                    _logger.LogDebug("Bumped cache version scope: {Scope}", scope);
                }

                return response;
            }


            if (request is IInvalidateCacheRequest invalidateReq)
            {
                await _redisService.RemoveAsync(invalidateReq.CacheKey);
                _logger.LogDebug("Cache data with cache key: {CacheKey} removed.", invalidateReq.CacheKey);
            }

            return response;
        }
    }
}
