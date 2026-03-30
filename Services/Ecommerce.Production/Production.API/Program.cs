using BuildingBlocks.Logging;
using Consul;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Production.API;
using Production.API.Middleware;
using Production.Application;
using Production.Application.Commons.Options;
using Production.Infrastructure;
using Production.Infrastructure.Data;
using System;
using System.Net;
using System.Net.Sockets;



var builder = WebApplication.CreateBuilder(args);
builder.Services
    .AddPresentation(builder.Configuration)  
    .AddApplication()                      
    .AddInfrastructure(builder.Configuration);

var registrationId = $"{builder.Environment.ApplicationName}-{builder.Environment.EnvironmentName}";

builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(7001, o =>
    {
        o.Protocols = HttpProtocols.Http2;
    });

    options.ListenAnyIP(7003, o =>
    {
        o.Protocols = HttpProtocols.Http1AndHttp2;
        o.UseHttps();
    });
}).UseKestrel();

//Cors policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy
            .WithOrigins("http://localhost:3000")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});


// --- OBSERVABILITY (OpenTelemetry) ---
const string serviceName = "ProductCatalogService";

builder.Logging.AddOpenTelemetry(logging =>
{
    logging.IncludeFormattedMessage = true;
    logging.IncludeScopes = true;
});

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService(serviceName))
    .WithTracing(tracing =>
    {
        tracing
           .AddAspNetCoreInstrumentation()
           .AddHttpClientInstrumentation()
           .AddEntityFrameworkCoreInstrumentation()
           .AddOtlpExporter(options =>
           {
               options.Endpoint = new Uri("http://jaeger:4317");
           });
    });

// --- HEALTH CHECKS ---
builder.Services.AddHealthChecks()
    // Database health check
    .AddCheck<Production.Infrastructure.HealthChecks.DatabaseHealthCheck>(
        "database",
        failureStatus: Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Unhealthy,
        tags: new[] { "db", "sql", "postgresql" })

    // Redis health check
    .AddCheck<Production.Infrastructure.HealthChecks.RedisCacheHealthCheck>(
        "redis_cache",
        failureStatus: Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Degraded,
        tags: new[] { "cache", "redis" })

    // RabbitMQ health check
    //.AddRabbitMQ(
    //    rabbitConnectionString: builder.Configuration.GetConnectionString("RabbitMQ") ?? "amqp://guest:guest@rabbitmq:5672",
    //    name: "rabbitmq",
    //    failureStatus: Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Degraded,
    //    tags: new[] { "messaging", "rabbitmq" })

     //Elasticsearch health check
    //.AddElasticsearch(
    //    builder.Configuration.GetConnectionString("Elasticsearch") ?? "http://elasticsearch:9200",
    //    name: "elasticsearch",
    //    failureStatus: Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Degraded,
    //    tags: new[] { "search", "elasticsearch" });

    // Self health check (memory, CPU)
    .AddCheck("self", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy("Service is running"),
        tags: new[] { "self" });

builder.Services.AddHangfire(config =>
{
    config
        .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
        .UseSimpleAssemblyNameTypeSerializer()
        .UseRecommendedSerializerSettings()
        .UsePostgreSqlStorage(options =>
        {
            options.UseNpgsqlConnection(builder.Configuration.GetConnectionString("DefaultConnection"));
        });


});

builder.Services.AddHangfireServer(options =>
{
    options.WorkerCount = 5; // Number of concurrent jobs
    options.ServerName = $"ProductCatalogService-{Environment.MachineName}";
});


// 6. Background Services
//builder.Services.AddHostedService<Production.Infrastructure.BackgroundServices.ElasticsearchSyncService>();
builder.Services.AddHostedService<Production.Infrastructure.BackgroundServices.CacheWarmingService>();
builder.Services.AddHostedService<Production.Infrastructure.BackgroundServices.InventoryMonitoringService>();
builder.Services.AddHostedService<Production.Infrastructure.BackgroundServices.ElasticsearchSyncService>();

builder.Services.AddScoped<Production.Infrastructure.Jobs.Scheduled.ProductCatologjobs>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


builder.Services.AddHealthChecksUI(options =>
{
    options.SetEvaluationTimeInSeconds(30); // Check every 30 seconds
    options.MaximumHistoryEntriesPerEndpoint(50);
    options.AddHealthCheckEndpoint("ProductCatalogService", "http://productAPI:8080/health");
})
.AddInMemoryStorage();

//Add Consul service discovery
builder.Services.Configure<ConsulOptions>(
    builder.Configuration.GetSection(ConsulOptions.SectionName));
builder.Services.AddSingleton<IConsulClient, ConsulClient>(sp =>
{
    // Lấy giá trị Options đã được bind từ appsettings
    var options = sp.GetRequiredService<IOptions<ConsulOptions>>().Value;

    return new ConsulClient(config =>
    {
        config.Address = new Uri(options.Address);
    });
});

SerilogConfig.Configure(builder, "ProductService");
var app = builder.Build();

var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();
var consulClient = app.Services.GetRequiredService<IConsulClient>();


var registrationID = "productservice-v1";

var serviceHost = Environment.GetEnvironmentVariable("SERVICE_HOST") ?? "productAPI";
var servicePort = int.Parse(Environment.GetEnvironmentVariable("SERVICE_PORT") ?? "8080");

var registration = new AgentServiceRegistration
{
    ID = registrationID,
    Name = "productservice",
    Address = serviceHost,
    Port = servicePort,
    Tags = new[] { "productservice", "catalog" },

    Check = new AgentServiceCheck
    {
        HTTP = $"http://{serviceHost}:{servicePort}/health",
        Interval = TimeSpan.FromSeconds(10),
        Timeout = TimeSpan.FromSeconds(5),
        DeregisterCriticalServiceAfter = TimeSpan.FromSeconds(30)
    }
};

lifetime.ApplicationStarted.Register(async () =>
{
    try
    {
        await consulClient.Agent.ServiceRegister(registration);
        Console.WriteLine("Register to Consul successfully!");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Consul Error: {ex.Message}");
    }
});

lifetime.ApplicationStopped.Register(() =>
{
    consulClient.Agent.ServiceDeregister(registrationID).Wait();
});

// service grpc 
app.MapGrpcService<Production.API.Services.ProductServiceImpl>(); 



app.UseCustomMiddleware();
app.UseHttpsRedirection();

// --- HANGFIRE DASHBOARD ---
// Access at: http://localhost:8080/hangfire
app.UseHangfireDashboard("/hangfire", new Hangfire.DashboardOptions
{
    DashboardTitle = "Product Catalog - Background Jobs",
    StatsPollingInterval = 2000, // 2 seconds
});

// Configure recurring jobs
Production.Infrastructure.Jobs.Scheduled.HangfireJobScheduler.ConfigureReCurringjobs();

//app.UseHttpsRedirection();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Product Catalog API V1");
        options.SwaggerEndpoint("/swagger/v2/swagger.json", "Product Catalog API V2");
        options.DisplayRequestDuration();
    });
}
app.UseCors("AllowFrontend");
app.MapControllers();
app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    ResponseWriter = HealthChecks.UI.Client.UIResponseWriter.WriteHealthCheckUIResponse,
    ResultStatusCodes =
    {
        [Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Healthy] = StatusCodes.Status200OK,
        [Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Degraded] = StatusCodes.Status200OK,
        [Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable
    }
});
app.MapHealthChecksUI(options =>
{
    options.UIPath = "/health-ui";
    options.ApiPath = "/health-ui-api";
});

using (var scope = app.Services.CreateScope())
{
    var seeder = scope.ServiceProvider.GetRequiredService<ProductSeeder>();
    await seeder.SeedAsync();
}
app.Run();
