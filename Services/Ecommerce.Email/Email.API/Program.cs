
using BuildingBlocks.Logging;
using BuildingBlocks.Messaging.MassTransit;
using BuildingBlocks.Observability.Swagger;
using Email.API.Options;
using Email.API.Services;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;


builder.Services.AddControllers();
builder.Services.AddTransient<IEmailService, EmailService>();
builder.Services.Configure<EmailSetting>(builder.Configuration.GetSection("EmailSetting"));
builder.Services.AddCustomSwagger();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddMessageBroker(configuration, Assembly.GetExecutingAssembly());
SerilogConfig.Configure(builder, "EmailService");
var app = builder.Build();



app.UseHttpsRedirection();

app.UseAuthorization();

var apiVersionProvider = app.Services.GetRequiredService<IApiVersionDescriptionProvider>();
app.UseCustomSwagger(apiVersionProvider);
app.MapControllers();

app.Run();
