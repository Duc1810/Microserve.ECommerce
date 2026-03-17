using BuildingBlocks.Messaging.MassTransit;
using BuildingBlocks.Repository;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Order.Application.Abstractions;
using Order.Application.Sagas;
using Order.Infrastructure.Data;
using Order.Infrastructure.Data.Interceptors;
using Order.Infrastructure.Service.Clients;
using System.Reflection;
namespace Order.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructureServices
            (this IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection");

            services.AddScoped<ISaveChangesInterceptor, AuditableEntityInterceptor>();
            services.AddScoped<ISaveChangesInterceptor, DispatchDomainEventsInterceptor>();
            services.AddScoped<DbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
            services.AddDbContext<ApplicationDbContext>((sp, options) =>
            {
                options.AddInterceptors(sp.GetServices<ISaveChangesInterceptor>());
                options.UseNpgsql(connectionString);
            });

            //service cart client
            services.AddScoped<ICartServiceClient, CartServiceClient>();

            services.AddScoped<IApplicationDbContext, ApplicationDbContext>();

            services.AddMessageBroker(configuration, Assembly.GetExecutingAssembly(),
               configureExtra: x =>
               {
                   x.AddEntityFrameworkOutbox<ApplicationDbContext>(o =>
                   {
                       o.UsePostgres();
                       o.UseBusOutbox();
                   });

                   x.AddSagaStateMachine<OrderStateMachine, OrderState>()
                        .EntityFrameworkRepository(r =>
                        {
                            r.ExistingDbContext<ApplicationDbContext>();
                            r.UsePostgres();
                        });

               });
            return services;
        }
    }
}
