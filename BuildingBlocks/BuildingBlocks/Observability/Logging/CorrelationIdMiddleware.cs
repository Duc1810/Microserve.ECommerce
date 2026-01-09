using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace BuildingBlocks.Logging;

public class SafeCorrelationIdMiddleware
{
    public const string HeaderName = "X-Correlation-ID";
    private readonly RequestDelegate _next;
    private readonly ILogger<SafeCorrelationIdMiddleware> _logger;
    private static readonly HashSet<string> SensitiveHeaders =
        new(StringComparer.OrdinalIgnoreCase) { "Authorization", "X-Api-Key", "ApiKey", "Token", "Password" };

    public SafeCorrelationIdMiddleware(RequestDelegate next, ILogger<SafeCorrelationIdMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task Invoke(HttpContext context)
    {
        var correlationId = context.Request.Headers.TryGetValue(HeaderName, out var cid) && !string.IsNullOrWhiteSpace(cid)
            ? (cid.ToString().Length > 128 ? cid.ToString().Substring(0, 128) : cid.ToString())
            : Guid.NewGuid().ToString("N");
        var started = false;
        if (Activity.Current == null)
        {
            Activity.Current = new Activity("http_request");
            Activity.Current.Start();
            started = true;
        }

        context.Response.Headers[HeaderName] = correlationId;

        Dictionary<string, string>? maskedHeaders = null;
        var needHeaders = _logger.IsEnabled(LogLevel.Debug); 

        if (needHeaders)
        {
            maskedHeaders = new Dictionary<string, string>(capacity: context.Request.Headers.Count, comparer: StringComparer.OrdinalIgnoreCase);
            foreach (var kv in context.Request.Headers)
            {
                var name = kv.Key;
                var value = kv.Value.ToString(); // StringValues -> string
                maskedHeaders[name] = SensitiveHeaders.Contains(name) ? Mask(value) : value;
            }
        }

        using (LogContext.PushProperty("CorrelationId", correlationId))
        using (LogContext.PushProperty("TraceId", Activity.Current?.TraceId.ToString()))
        using (needHeaders ? LogContext.PushProperty("RequestHeaders", maskedHeaders!, destructureObjects: true) : null!)
        {
            try
            {
                await _next(context);
            }
            finally
            {
                if (started) Activity.Current?.Stop();
            }
        }
    }

    private static string Mask(string value)
        => string.IsNullOrEmpty(value) ? value : (value.Length <= 4 ? "****" : new string('*', value.Length - 4) + value[^4..]);
}
