using BuildingBlocks.Logging;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Production.API;
using Production.Application;
using Production.Infrastructure;



var builder = WebApplication.CreateBuilder(args);
builder.Services
    .AddPresentation(builder.Configuration)  
    .AddApplication()                      
    .AddInfrastructure(builder.Configuration);



builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(7001, o =>
    {
        o.Protocols = HttpProtocols.Http2;
    });

    options.ListenAnyIP(7003, o =>
    {
        o.Protocols = HttpProtocols.Http1AndHttp2;
    });
}).UseKestrel();
SerilogConfig.Configure(builder, "ProductService");
var app = builder.Build();

await app.Services.SeedAsync();
app.UsePresentation();
app.Run();
