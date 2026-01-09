using Authentication.Application;
using Authentication.Domain.Entities;
using Authentication.Infrastructure;
using Authentication.Infrastructure.Data;
using Authentication.Infrastructure.Data.Configurations;
using Authentication.Infrastructure.Token.Options;
using BuildingBlocks.Observability.Behaviors;
using Duende.IdentityServer.Services;
using Duende.IdentityServer.Stores;
using FluentValidation;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using System.Reflection;

namespace Authentication.API.Configuaration
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddDataProtectionWithFileStore(
            this IServiceCollection services, IConfiguration config, IHostEnvironment env)
        {
            var dpKeysPath = Path.Combine(env.ContentRootPath, "dp_keys");
            Directory.CreateDirectory(dpKeysPath);

            services.AddDataProtection()
                .PersistKeysToFileSystem(new DirectoryInfo(dpKeysPath))
                .SetApplicationName("AuthServerDev");

            return services;
        }

        public static IServiceCollection AddForwardedHeadersConfig(this IServiceCollection services)
        {
            services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            });
            return services;
        }

        public static IServiceCollection AddIdentityWithStores(this IServiceCollection services)
        {
            services.AddIdentity<User, IdentityRole>(options =>
            {
                options.Password.RequiredLength = 6;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireDigit = false;
                options.User.RequireUniqueEmail = true;

                options.ClaimsIdentity.RoleClaimType = "role";
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

            return services;
        }

        public static IServiceCollection AddIdentityServerConfigured(this IServiceCollection services, IConfiguration config)
        {
            services.Configure<IdentityServerClientOptions>(config.GetSection("AuthOptions"));
            services.AddSingleton<IClientStore, ClientStore>();
            services.AddTransient<ClientStore>();
            services.AddTransient<ResourceStore>();
            services.AddScoped<IProfileService, ProfileService>();

            services.AddIdentityServer(options =>
            {
                //options.IssuerUri = "https://localhost:7202";
                options.KeyManagement.Enabled = false;
                options.Events.RaiseErrorEvents = true;
                options.Events.RaiseFailureEvents = true;
                options.Events.RaiseSuccessEvents = true;
                options.Events.RaiseInformationEvents = true;
            })
            .AddAspNetIdentity<User>()
            .AddClientStore<ClientStore>()
            .AddResourceStore<ResourceStore>()
            .AddResourceOwnerValidator<UserValidator>()
            .AddProfileService<ProfileService>()
            .AddDeveloperSigningCredential(persistKey: true, filename: Path.Combine(AppContext.BaseDirectory, "tempkey.jwk"))
            .AddInMemoryPersistedGrants();

            services.AddAuthorization();
            return services;
        }

        public static IServiceCollection AddMediatRAndValidation(this IServiceCollection services)
        {
            var appAssembly = Assembly.Load("Authentication.Application");

            services.AddMediatR(cfg =>
            {
                cfg.RegisterServicesFromAssembly(appAssembly);
                cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
                cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
            });

            services.AddValidatorsFromAssembly(appAssembly);

            return services;
        }

        public static IServiceCollection AddInfrastructureAndApp(this IServiceCollection services, IConfiguration config)
        {
            services.AddDatabase(config);
            services.AddApplicationServices(); 
            return services;
        }

       
    }
}
