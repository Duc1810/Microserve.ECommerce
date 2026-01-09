using Authentication.Application.Abstractions;
using Authentication.Domain.Entities;
using Authentication.Infrastructure.Data;
using Authentication.Infrastructure.Data.Configurations;
using Authentication.Infrastructure.Token;
using Authentication.Infrastructure.Token.Options;
using BuildingBlocks.Messaging.MassTransit;
using Duende.IdentityServer.Services;
using Duende.IdentityServer.Stores;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Reflection;

namespace Authentication.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddDatabase(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection");

            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseNpgsql(connectionString));

            services.AddScoped<ILogoutService, TokenService>();
            services.AddHttpClient<Application.Abstractions.ITokenService, TokenService>()
                .ConfigurePrimaryHttpMessageHandler(() =>
                    new HttpClientHandler
                    {
                        ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                    });

            return services;
        }


        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config, IHostEnvironment env)
        {
            // Database
            services.AddDatabase(config);

            // Identity + Stores
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

            // IdentityServer
            services.Configure<IdentityServerClientOptions>(config.GetSection("AuthOptions"));
            services.AddSingleton<IClientStore, ClientStore>();
            services.AddTransient<ClientStore>();
            services.AddTransient<ResourceStore>();
            services.AddScoped<IProfileService, ProfileService>();

            services.AddIdentityServer(options =>
            {
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

            // Messaging
            services.AddMessageBroker(config, Assembly.GetExecutingAssembly());
            services.AddEventBus();

            return services;
        }
    }
}

