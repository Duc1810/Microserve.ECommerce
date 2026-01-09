
using System.ComponentModel.DataAnnotations;


namespace Cart.Infrastructure.Http;
    public sealed class ResilienceOptions
    {
        public int[] RetryDelaysMs { get; init; } = new[] { 200, 500, 1000 };
        public bool RespectRetryAfter { get; init; } = true;
        [Range(1, 100)] public int CircuitBreakerFailures { get; init; } = 5;
        [Range(1, 600)] public int CircuitBreakerDurationSeconds { get; init; } = 30;
    }

