//using Authentication.API;
//using Authentication.API.Configuaration;
//using BuildingBlocks.Identity;
//using BuildingBlocks.Logging;
//using BuildingBlocks.Messaging.MassTransit;
//using BuildingBlocks.Observability.Authentication;
//using BuildingBlocks.Observability.Exceptions.Handler;
//using BuildingBlocks.Observability.Swagger;
//using Microsoft.AspNetCore.Mvc.ApiExplorer;
//var builder = WebApplication.CreateBuilder(args);
//builder.Services
//    .AddDataProtectionWithFileStore(builder.Configuration, builder.Environment)
//    .AddForwardedHeadersConfig()
//    .AddInfrastructureAndApp(builder.Configuration) 
//    .AddIdentityWithStores()
//    .AddIdentityServerConfigured(builder.Configuration)
//    .AddMediatRAndValidation();
//builder.Services.AddControllers();
//builder.Services.AddExceptionHandler<CustomExceptionHandler>();
//builder.Services.AddProblemDetails();
//builder.Services.AddCurrentUser();
//builder.Services.AddMessageBroker(builder.Configuration, typeof(Program).Assembly);
//builder.Services.AddEventBus();
//builder.Services.AddJwtAuthWithManualJwks(builder.Configuration);
//builder.Services.AddCustomSwagger();
//builder.Services.AddEndpointsApiExplorer();
//builder.Services.AddSwaggerGen();
//SerilogConfig.Configure(builder, "AuthService");
//var app = builder.Build();

//// Seed
//await DbInitializer.SeedAsync(app);
//// --- Pipeline ---
//app.UseRouting();
////app.UseHttpsRedirection();
//app.UseExceptionHandler();
//app.UseForwardedHeaders();
//app.UseIdentityServer();
//app.UseAuthentication();
//app.UseAuthorization();
//var apiVersionProvider = app.Services.GetRequiredService<IApiVersionDescriptionProvider>();
//app.UseCustomSwagger(apiVersionProvider);

//app.MapControllers();
//app.Run();


using Authentication.API;
using Authentication.API.Configurations;
using Authentication.API.Middleware;
using Authentication.Application;
using Authentication.Infrastructure;
using BuildingBlocks.Logging;
using BuildingBlocks.Observability.Swagger;
using MassTransit;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Serilog;

var builder = WebApplication.CreateBuilder(args);
// Configure Serilog to read configuration from appsettings.json
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(new ConfigurationBuilder()
        .AddJsonFile("appsettings.json")
        .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}.json", optional: true)
        .Build())
    .Enrich.FromLogContext()
    .CreateLogger();
// Services
builder.Services
    .AddApiServices(builder.Configuration, builder.Environment)
    .AddApplicationServices()
    .AddInfrastructure(builder.Configuration, builder.Environment);

builder.Services.AddRateLimiter(static rateLimiterOptions =>
{
    rateLimiterOptions.GlobalLimiter = System.Threading.RateLimiting.PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
    {
        var userUdentifieer = httpContext.User.Identity?.Name ?? httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous";

        return System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: userUdentifieer,
            factory: _ => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = System.Threading.RateLimiting.QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            });
    });

    //Strict rate limiter for autentication endpoints to prevent brute-force attacks
    rateLimiterOptions.AddPolicy("auth", context =>
        System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
            factory: _ => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = System.Threading.RateLimiting.QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            }));

    //Token window limiter for refresh token endpoint to prevent abuse while allowing legitimate use cases
    rateLimiterOptions.AddPolicy("refresh", context =>
            System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: context.User.Identity?.Name ?? context.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
                factory: _ => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
                {
                    PermitLimit = 10,
                    Window = TimeSpan.FromMinutes(1),
                    QueueProcessingOrder = System.Threading.RateLimiting.QueueProcessingOrder.OldestFirst,
                    QueueLimit = 2
                }));

    rateLimiterOptions.AddPolicy("concurrent", context =>
           System.Threading.RateLimiting.RateLimitPartition.GetConcurrencyLimiter(
               partitionKey: context.User.Identity?.Name ?? "anonymous",
               factory: _ => new System.Threading.RateLimiting.ConcurrencyLimiterOptions
               {
                   PermitLimit = 10,
                   QueueProcessingOrder = System.Threading.RateLimiting.QueueProcessingOrder.OldestFirst,
                   QueueLimit = 5
               }));

    rateLimiterOptions.OnRejected = async (context, cancellatinoToken) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;

        TimeSpan? retryAfterValue = null;
        if (context.Lease.TryGetMetadata(System.Threading.RateLimiting.MetadataName.RetryAfter, out var retryAfter))
        {
            retryAfterValue = retryAfter;
            context.HttpContext.Response.Headers.RetryAfter = ((int)retryAfter.TotalSeconds).ToString();
        }

        await context.HttpContext.Response.WriteAsJsonAsync(new
        {
            error = "Too many requests",
            message = "Rate limit exceeded. Please try again later.",
            retryAfterSeconds = retryAfterValue.HasValue ? (int)retryAfterValue.Value.TotalSeconds : (int?)null
        }, cancellatinoToken);
    };
});

SerilogConfig.Configure(builder, "AuthService");

var app = builder.Build();

// Seed
await DbInitializer.SeedAsync(app);

// Pipeline
app.UseRouting();
app.UseHttpsRedirection();
app.UseMiddleware<SecurityHeaderMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseExceptionHandler();
app.UseForwardedHeaders();
app.UseIdentityServer();
app.UseAuthentication();
app.UseAuthorization();


var apiVersionProvider = app.Services.GetRequiredService<IApiVersionDescriptionProvider>();
app.UseCustomSwagger(apiVersionProvider);


app.MapControllers();
app.Run();
