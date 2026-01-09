using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace BuildingBlocks.Observability.Swagger;

public static class SwaggerExtensions
{
    public static IServiceCollection AddCustomSwagger(this IServiceCollection services)
    {
        // API Versioning + Explorer
        services.AddApiVersioning(opt =>
        {
            opt.AssumeDefaultVersionWhenUnspecified = true;
            opt.DefaultApiVersion = new ApiVersion(1, 0);
            opt.ReportApiVersions = true;
        });

        services.AddVersionedApiExplorer(opt =>
        {
            opt.GroupNameFormat = "'v'VVV";     // v1, v2
            opt.SubstituteApiVersionInUrl = true;
        });

        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();

        // Đăng ký cấu hình SwaggerGen dựa vào provider
        services.AddTransient<IConfigureOptions<SwaggerGenOptions>, ConfigureSwaggerOptions>();

        return services;
    }

    public static IApplicationBuilder UseCustomSwagger(this IApplicationBuilder app, IApiVersionDescriptionProvider provider)
    {
        app.UseSwagger(c => { c.SerializeAsV2 = false; });

        app.UseSwaggerUI(opt =>
        {
            foreach (var desc in provider.ApiVersionDescriptions.OrderBy(d => d.ApiVersion))
            {
                opt.SwaggerEndpoint($"/swagger/{desc.GroupName}/swagger.json",
                                    $" API {desc.GroupName.ToUpperInvariant()}");
            }
            opt.RoutePrefix = "swagger";
            opt.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.List);
            opt.DocumentTitle = " API Docs";
        });

        return app;
    }
}
