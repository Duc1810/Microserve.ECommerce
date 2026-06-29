using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.OpenApi.Models;

namespace Dashboard.API;

public static class DependencyInjection
{
    public static IServiceCollection AddApiServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Controllers
        services.AddControllers();
        
        // API Explorer
        services.AddEndpointsApiExplorer();
        
        // Swagger
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo 
            { 
                Title = "Dashboard API", 
                Version = "v1",
                Description = "Ecommerce Dashboard API for aggregated metrics and analytics"
            });
            
            // JWT Authentication for Swagger
            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.ApiKey,
                Scheme = "Bearer"
            });
            
            c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });
        });
        
        // JWT Authentication
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = configuration["Jwt:Issuer"],
                    ValidAudience = configuration["Jwt:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key not configured")))
                };
            });
        
        // Authorization
        services.AddAuthorization(options =>
        {
            options.AddPolicy("AdminOnly", policy => 
                policy.RequireRole("Admin"));
        });
        
        // CORS
        services.AddCors(options =>
        {
            options.AddDefaultPolicy(builder =>
            {
                builder.AllowAnyOrigin()
                       .AllowAnyMethod()
                       .AllowAnyHeader();
            });
        });
        
        // Health Checks
        services.AddHealthChecks();
        
        return services;
    }
}