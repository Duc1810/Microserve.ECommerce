using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Consul;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BuildingBlocks.Consul;

public sealed class ConsulHostedService(
    IConsulClient consul,
    IOptions<ConsulOptions> options,
    ILogger<ConsulHostedService> logger
) : IHostedService
{
    private readonly IConsulClient _consul = consul;
    private readonly ConsulOptions _cfg = options.Value;
    private readonly ILogger<ConsulHostedService> _log = logger;

    private AgentServiceRegistration? _registration;

    public async Task StartAsync(CancellationToken ct)
    {
        // Lấy URL service hiện đang lắng nghe (vd: http://authAPI:8080)
        var current = ResolveSelfUrl();
        var serviceId = string.IsNullOrWhiteSpace(_cfg.Id)
            ? $"{_cfg.Name}-{Guid.NewGuid():N}"
            : _cfg.Id;

        _registration = new AgentServiceRegistration
        {
            ID = serviceId,
            Name = _cfg.Name,
            Address = current.Host,     // QUAN TRỌNG: host gateway truy cập được (vd: authAPI)
            Port = current.Port,        // ví dụ 8080
            Check = new AgentServiceCheck
            {
                HTTP = new Uri(current, _cfg.HealthCheckEndPoint).ToString(),
                Interval = TimeSpan.FromSeconds(10),
                Timeout = TimeSpan.FromSeconds(5),
                TLSSkipVerify = true
            }
        };

        _log.LogInformation("[Consul] Register {Name} at {Host}:{Port} (health: {Health})",
            _registration.Name, _registration.Address, _registration.Port, _registration.Check?.HTTP);

        // Đăng ký với retry có kiểm soát (tránh race khi Consul chưa sẵn)
        await RegisterWithRetryAsync(ct).ConfigureAwait(false);
    }

    public async Task StopAsync(CancellationToken ct)
    {
        if (_registration is null) return;

        try
        {
            await _consul.Agent.ServiceDeregister(_registration.ID, ct).ConfigureAwait(false);
            _log.LogInformation("[Consul] Deregistered {ID}", _registration.ID);
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "[Consul] Deregister failed for {ID}", _registration.ID);
        }
    }

    // ------- Helpers -------

    private async Task RegisterWithRetryAsync(CancellationToken ct)
    {
        if (_registration is null) return;

        var attempt = 0;
        var maxDelay = TimeSpan.FromSeconds(15);
        while (!ct.IsCancellationRequested)
        {
            try
            {
                await _consul.Agent.ServiceRegister(_registration, ct).ConfigureAwait(false);
                _log.LogInformation("[Consul] Registered {Name}", _registration.Name);
                return;
            }
            catch (HttpRequestException ex) when (!ct.IsCancellationRequested)
            {
                attempt++;
                var delay = TimeSpan.FromSeconds(Math.Min(2 * attempt, (int)maxDelay.TotalSeconds));
                _log.LogWarning(ex, "[Consul] Register failed (attempt {Attempt}) -> retry in {Delay}s",
                    attempt, delay.TotalSeconds);
                try { await Task.Delay(delay, ct).ConfigureAwait(false); } catch { return; }
            }
        }
    }

    private static Uri ResolveSelfUrl()
    {
        // Ưu tiên ENV SERVICE__URL nếu bạn set thẳng (vd: http://authAPI:8080)
        var envUrl = Environment.GetEnvironmentVariable("SERVICE__URL");
        if (!string.IsNullOrWhiteSpace(envUrl) && Uri.TryCreate(envUrl, UriKind.Absolute, out var u1))
            return u1;

        // Chuẩn ASP.NET Core: ASPNETCORE_URLS (vd: "http://+:8080" hoặc "http://0.0.0.0:8080")
        var urls = Environment.GetEnvironmentVariable("ASPNETCORE_URLS");
        if (!string.IsNullOrWhiteSpace(urls))
        {
            // Lấy url đầu tiên & chuẩn hoá host về "localhost" nếu là + hoặc 0.0.0.0
            var first = urls.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)[0];
            if (Uri.TryCreate(first, UriKind.Absolute, out var raw))
            {
                var host = (raw.Host is "+" or "0.0.0.0" or "[::]") ? "localhost" : raw.Host;
                return new Uri($"{raw.Scheme}://{host}:{raw.Port}");
            }
        }

        // Cuối cùng: fallback (dev)
        return new Uri("http://localhost:8080");
    }
}
