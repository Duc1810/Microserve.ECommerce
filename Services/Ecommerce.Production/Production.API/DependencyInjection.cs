using BuildingBlocks.Observability.Authentication;
using BuildingBlocks.Observability.Exceptions.Handler;
using BuildingBlocks.Observability.Swagger;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Production.API.Services;

namespace Production.API;

public static class Presentation
{
    public static IServiceCollection AddPresentation(this IServiceCollection services, IConfiguration cfg)
    {
        services.AddControllers();
        services.AddExceptionHandler<CustomExceptionHandler>();
        services.AddProblemDetails();
        services.AddSwaggerGen();
        services.AddJwtAuthWithManualJwks(cfg);
        services.AddEndpointsApiExplorer();
        services.AddCustomSwagger();
        services.AddGrpc();
        return services;
    }

    public static IApplicationBuilder UsePresentation(this IApplicationBuilder app)
    {
        app.UseExceptionHandler();
        app.UseAuthentication();
        app.UseAuthorization();

        // Swagger UI
        var provider = app.ApplicationServices.GetRequiredService<IApiVersionDescriptionProvider>();
        app.UseCustomSwagger(provider);

        // Endpoints
        var web = (WebApplication)app;
        web.MapGrpcService<ProductServiceImpl>();
        web.MapControllers();

        return app;
    }
}
