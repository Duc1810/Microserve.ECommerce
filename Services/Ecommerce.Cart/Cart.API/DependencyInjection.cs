using BuildingBlocks.Identity;
using BuildingBlocks.Observability.Authentication;
using BuildingBlocks.Observability.Exceptions.Handler;
using BuildingBlocks.Observability.Swagger;

namespace Cart.API;

public static class DependencyInjection
{
    public static IServiceCollection AddPresentation(this IServiceCollection services, IConfiguration config)
    {
        services.AddExceptionHandler<CustomExceptionHandler>();
        services.AddProblemDetails();

        services.AddJwtAuthWithManualJwks(config);
        services.AddCurrentUser();      
        services.AddSwaggerGen();
        services.AddControllers();
        services.AddEndpointsApiExplorer();
        services.AddCustomSwagger();
       
        return services;
    }
}
