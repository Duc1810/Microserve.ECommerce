using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;



namespace BuildingBlocks.EFCore
{
    public static class Extension
    {
        public static IServiceCollection AddCustomDbContext<TContext>(
            this IServiceCollection services,
            IConfiguration configuration)
            where TContext : DbContext
        {
            AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

            var databaseOptions = new DatabaseOptions
            {
                DefaultConnection = configuration.GetConnectionString("DefaultConnection")!
            };

            services.AddDbContextPool<TContext>((sp, options) =>
            {
                options.UseNpgsql(
                    databaseOptions.DefaultConnection,
                    dbOptions =>
                    {
                        dbOptions.MigrationsAssembly(typeof(TContext).Assembly.GetName().Name);
                    })
                .UseSnakeCaseNamingConvention();
            });

            return services;
        }


        public static IApplicationBuilder UseMigration<TContext>(this IApplicationBuilder app)
            where TContext : DbContext
        {
            MigrateDatabaseAsync<TContext>(app.ApplicationServices).GetAwaiter().GetResult();
            SeedDataAsync(app.ApplicationServices).GetAwaiter().GetResult();
            return app;
        }

        private static async Task MigrateDatabaseAsync<TContext>(IServiceProvider serviceProvider)
            where TContext : DbContext
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<TContext>();
            await context.Database.MigrateAsync();
        }

        private static async Task SeedDataAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var seeders = scope.ServiceProvider.GetServices<IDataSeeder>();
            foreach (var seeder in seeders)
            {
                await seeder.SeedAllAsync();
            }
        }
    }


}
