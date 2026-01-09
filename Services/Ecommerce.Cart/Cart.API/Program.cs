using BuildingBlocks.Logging;
using BuildingBlocks.Observability.Swagger;
using Cart.API;
using Cart.Application;
using Cart.Infrastructure;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Server.Kestrel.Core;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost
    .ConfigureKestrel(opt =>
    {
        opt.ListenAnyIP(5027, o =>
        {
            o.Protocols = HttpProtocols.Http1;
        });
    })
    .UseKestrel();
SerilogConfig.Configure(builder, "CartService");
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplication();
builder.Services.AddPresentation(builder.Configuration);
var app = builder.Build();
app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseAuthentication();
app.UseAuthorization();
var apiVersionProvider = app.Services.GetRequiredService<IApiVersionDescriptionProvider>();
app.UseCustomSwagger(apiVersionProvider);
app.MapControllers();
app.Run();





//builder.Services.AddMediatR(cfg =>
//{
//    cfg.RegisterServicesFromAssemblyContaining<CreateCartItemCommand>();
//    cfg.RegisterServicesFromAssemblyContaining<GetCartByUserQuery>();
//    cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
//    cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
//});
//builder.Services.AddValidatorsFromAssemblyContaining<CreateCartItemCommandValidator>();
//builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);
//var builder = WebApplication.CreateBuilder(args);
//builder.WebHost
//    .ConfigureKestrel(opt =>
//    {
//        opt.ListenAnyIP(5027, o =>
//        {
//            o.Protocols = HttpProtocols.Http1;
//        });
//    })
//    .UseKestrel();