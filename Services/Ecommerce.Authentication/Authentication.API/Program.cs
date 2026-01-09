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
using Authentication.Application;
using Authentication.Infrastructure;
using BuildingBlocks.Logging;
using BuildingBlocks.Observability.Swagger;
using Microsoft.AspNetCore.Mvc.ApiExplorer;

var builder = WebApplication.CreateBuilder(args);

// Services
builder.Services
    .AddApiServices(builder.Configuration, builder.Environment)
    .AddApplicationServices()
    .AddInfrastructure(builder.Configuration, builder.Environment);

SerilogConfig.Configure(builder, "AuthService");

var app = builder.Build();

// Seed
await DbInitializer.SeedAsync(app);

// Pipeline
app.UseRouting();
//app.UseHttpsRedirection();
app.UseExceptionHandler();
app.UseForwardedHeaders();
app.UseIdentityServer();
app.UseAuthentication();
app.UseAuthorization();

var apiVersionProvider = app.Services.GetRequiredService<IApiVersionDescriptionProvider>();
app.UseCustomSwagger(apiVersionProvider);

app.MapControllers();
app.Run();
