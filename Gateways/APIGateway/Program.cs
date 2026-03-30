using Consul;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Ocelot.Provider.Consul;
using Ocelot.Provider.Polly;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Serilog;

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration));

builder.Configuration
    .SetBasePath(builder.Environment.ContentRootPath)
    .AddJsonFile("Routers/ocelot.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"Routers/ocelot.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics =>
    {
        metrics.AddAspNetCoreInstrumentation() // Monitor inbound HTTP requests
               .AddPrometheusExporter(); // Enable the /metrics endpoint
    });

builder.Services
    .AddOcelot(builder.Configuration)
    .AddConsul() 
    .AddPolly();

builder.Services.AddHttpClient();


builder.Services.AddSingleton<IConsulClient>(_ =>
    new ConsulClient(c => c.Address = new Uri("http://consul:8500")));

var app = builder.Build();



app.UseRouting();

app.MapGet("/api/healths/{service}", async (string service, IConsulClient consul, IHttpClientFactory http) =>
{
    var healthResponse = await consul.Health.Service(service, tag: null, passingOnly: true);
    var instance = healthResponse.Response.FirstOrDefault();

    var addrress = $"{instance.Service.Address}:{instance.Service.Port}";
    var target = $"http://{addrress}/health";

    try
    {
        var client = http.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(5);
        var resp = await client.GetAsync(target);
        var body = await resp.Content.ReadAsStringAsync();

        return Results.Json(new
        {
            status = "Success",
            serviceName = service,
            resolvedAddress = addrress,
            downstreamHealthStatus = resp.StatusCode.ToString(),
            detail = body
        });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = $"Lỗi kết nối tới Downstream: {ex.Message}", resolvedAddress = addrress });
    }
});
app.UseOpenTelemetryPrometheusScrapingEndpoint();
await app.UseOcelot();

app.Run();