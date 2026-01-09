using BuildingBlocks.Identity;
using BuildingBlocks.Logging;
using BuildingBlocks.Observability.Authentication;
using BuildingBlocks.Observability.Exceptions.Handler;
using BuildingBlocks.Observability.Swagger;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
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
