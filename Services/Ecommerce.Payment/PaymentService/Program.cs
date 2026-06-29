using BuildingBlocks.EFCore;
using BuildingBlocks.Repository;
using Consul;
using Microsoft.EntityFrameworkCore;
using PaymentService.Data;
using PaymentService.Services;
using MassTransit;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Database Configuration
builder.Services.AddCustomDbContext<PaymentDbContext>(builder.Configuration);
builder.Services.AddScoped<DbContext>(provider => provider.GetRequiredService<PaymentDbContext>());

// Repository Pattern
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));

// Payment Services
builder.Services.AddScoped<ITransactionService, TransactionService>();
builder.Services.AddScoped<IPayOSService, PayOSService>();

// PayOS Configuration
builder.Services.Configure<PayOSConfig>(builder.Configuration.GetSection("PayOS"));

// MassTransit Configuration
builder.Services.AddMassTransit(x =>
{
    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(builder.Configuration.GetConnectionString("RabbitMQ"));

        // Configure retry policy
        cfg.UseMessageRetry(r => r.Exponential(
            retryLimit: 5,
            minInterval: TimeSpan.FromSeconds(1),
            maxInterval: TimeSpan.FromMinutes(5),
            intervalDelta: TimeSpan.FromSeconds(2)));

        cfg.ConfigureEndpoints(context);
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
