using BuildingBlocks.Identity;
using BuildingBlocks.Logging;
using BuildingBlocks.Messaging.MassTransit;
using BuildingBlocks.Observability.Authentication;
using BuildingBlocks.Observability.Behaviors;
using BuildingBlocks.Observability.Exceptions.Handler;
using BuildingBlocks.Observability.Swagger;
using FluentValidation;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.SignalR;
using Notification.API.Hubs;
using Notification.BLL.Commons.Ports;
using Notification.BLL.Features.Event;
using Notification.BLL.Features.Features.CreateNotification;
using Notification.BLL.Features.Notifications.Queries;
using Notification.Data;


var builder = WebApplication.CreateBuilder(args);


builder.Services.AddControllers();

builder.Services.AddDatabase(builder.Configuration);
builder.Services.AddExceptionHandler<CustomExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddJwtAuthWithManualJwks(builder.Configuration);
builder.Services.AddCurrentUser();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var assemblies = new[]
{
    typeof(NotificationController).Assembly,
    typeof(GetNotificationsHandler).Assembly,
    typeof(CreateNotificationHandler).Assembly,
};

builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssemblies(assemblies);
    cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
    cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
});

builder.Services.AddValidatorsFromAssemblies(assemblies);
builder.Services.AddSignalR();
builder.Services.AddSingleton<IUserIdProvider, SubUserIdProvider>();
builder.Services.AddScoped<INotificationDispatcher, SignalRNotificationDispatcher>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddCustomSwagger();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<INotificationDispatcher, SignalRNotificationDispatcher>();
SerilogConfig.Configure(builder, "NotificationService");
builder.Services.AddMessageBroker(
    builder.Configuration,
    typeof(NotificationCreatedEventConsumer).Assembly
);
builder.Services.AddCors(options =>
{
    options.AddPolicy("frontend7171", p => p
        .WithOrigins("https://localhost:7171")
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials()
    );
});


var app = builder.Build();
app.UseCors("frontend7171");
app.UseHttpsRedirection();
app.UseStatusCodePages();
app.UseAuthentication();
app.UseAuthorization();
var apiVersionProvider = app.Services.GetRequiredService<IApiVersionDescriptionProvider>();
app.UseCustomSwagger(apiVersionProvider);
app.MapControllers();
app.MapHub<NotificationHub>("/hubs/notifications");

app.Run();
