using System.Collections.Concurrent;
using System.Net;

namespace Production.API.Middleware;

public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RateLimitingMiddleware> _logger;
    private static readonly ConcurrentDictionary<string, ClientRequestInfo> _clients = new();
    private readonly int _requestLimit = 100; // Max requests per time window
    private readonly TimeSpan _timeWindow = TimeSpan.FromMinutes(1); // Time window for rate limiting
    public RateLimitingMiddleware(RequestDelegate next, ILogger<RateLimitingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }
    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Path.StartsWithSegments("/health") ||
           context.Request.Path.StartsWithSegments("/hangfire"))
        {
            await _next(context);
            return;
        }

        var clientId = GetClientIdentifier(context);
        var clientInfo = _clients.GetOrAdd(clientId, _ => new ClientRequestInfo());

        await clientInfo.Semaphore.WaitAsync();

        try
        {
            var now = DateTime.UtcNow;

            // Remove old requests outside the time window
            clientInfo.RequestTimestamps.RemoveAll(timestamp =>
                now - timestamp > _timeWindow);

            if (clientInfo.RequestTimestamps.Count >= _requestLimit)
            {
                _logger.LogWarning(
                    "Rate limit exceeded for client {ClientId}. Limit: {Limit} requests per {TimeWindow}",
                    clientId,
                    _requestLimit,
                    _timeWindow);

                context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
                context.Response.Headers.Append("Retry-After", _timeWindow.TotalSeconds.ToString());

                await context.Response.WriteAsJsonAsync(new
                {
                    statusCode = 429,
                    message = "Rate limit exceeded. Please try again later.",
                    retryAfter = _timeWindow.TotalSeconds
                });

                return;
            }

            clientInfo.RequestTimestamps.Add(now);
        }
        finally
        {
            clientInfo.Semaphore.Release();
        }

        // Add rate limit headers
        var remainingRequests = _requestLimit - clientInfo.RequestTimestamps.Count;
        context.Response.Headers.Append("X-RateLimit-Limit", _requestLimit.ToString());
        context.Response.Headers.Append("X-RateLimit-Remaining", remainingRequests.ToString());
        context.Response.Headers.Append("X-RateLimit-Reset",
            DateTime.UtcNow.Add(_timeWindow).ToString("o"));

        await _next(context);
    }

    private string GetClientIdentifier(HttpContext context)
    {
        // Try to get client IP from X-Forwarded-For header (for proxies/load balancers)
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            return forwardedFor.Split(',')[0].Trim();
        }

        // Fall back to remote IP address
        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}
public class ClientRequestInfo
{
    public List<DateTime> RequestTimestamps { get; set; } = new();
    public SemaphoreSlim Semaphore { get; } = new(1, 1);
}
