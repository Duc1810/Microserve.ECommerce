using Dashboard.API;
using Dashboard.Application;
using Dashboard.Infrastructure;
using BuildingBlocks.Logging;
using BuildingBlocks.Observability.Swagger;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(new ConfigurationBuilder()
        .AddJsonFile("appsettings.json")
        .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}.json", optional: true)
        .Build())
    .Enrich.FromLogContext()
    .CreateLogger();

// Services
builder.Services
    .AddApiServices(builder.Configuration)
    .AddApplication()
    .AddInfrastructure(builder.Configuration);

SerilogConfig.Configure(builder, "DashboardService");

var app = builder.Build();

// Pipeline
app.UseRouting();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    var apiVersionProvider = app.Services.GetRequiredService<IApiVersionDescriptionProvider>();
    app.UseCustomSwagger(apiVersionProvider);
}

app.MapControllers();
app.Run();