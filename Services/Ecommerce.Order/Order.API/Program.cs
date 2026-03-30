using BuildingBlocks.Identity;
using BuildingBlocks.Logging;
using BuildingBlocks.Observability.Authentication;
using BuildingBlocks.Observability.Exceptions.Handler;
using BuildingBlocks.Observability.Swagger;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Order.API.Helpers;
using Order.Application;
using Order.Infrastructure;
using Order.Infrastructure.Data;
using Ordering.Infrastructure.Data.Extensions;

var builder = WebApplication.CreateBuilder(args);



builder.Services.AddControllers();
builder.Services
    .AddApplicationServices(builder.Configuration)
    .AddInfrastructureServices(builder.Configuration);

builder.Services.AddExceptionHandler<CustomExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddJwtAuthWithManualJwks(builder.Configuration);
builder.Services.AddCurrentUser();

const string serviceName = "OrderService";

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

builder.Services.AddHttpContextAccessor();

// Register the handler as a transient service
builder.Services.AddTransient<AuthenticationHeaderHandler>();

// Configure the Named HttpClient
builder.Services.AddHttpClient("CartServiceUrl", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["CartServiceUrl"]!);
})
.AddHttpMessageHandler<AuthenticationHeaderHandler>()
.AddStandardResilienceHandler();

builder.Services.AddHttpClient("PaymentClient", client => client.BaseAddress = new Uri(builder.Configuration["PaymentServiceUrl"]!))
    .AddStandardResilienceHandler();

builder.Services.AddHttpClient("ProductClient", client => client.BaseAddress = new Uri(builder.Configuration["ProductCatalogServiceUrl"]!))
    .AddStandardResilienceHandler();

SerilogConfig.Configure(builder, "OrderService");
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddCustomSwagger();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    dbContext.Database.EnsureCreated(); 

    if (!dbContext.Customers.Any())
        dbContext.Customers.AddRange(InitialData.Customers);


    dbContext.SaveChanges();
}
app.UseExceptionHandler();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
var apiVersionProvider = app.Services.GetRequiredService<IApiVersionDescriptionProvider>();
app.UseCustomSwagger(apiVersionProvider);
app.MapControllers();

app.Run();
