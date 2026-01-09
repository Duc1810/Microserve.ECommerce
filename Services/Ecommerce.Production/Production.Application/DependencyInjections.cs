
namespace Production.Application
{
    public static class DependencyInjection
    {

        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            var assembly = Assembly.GetExecutingAssembly();

            services.AddMediatR(cfg =>
            {
                cfg.RegisterServicesFromAssembly(assembly);
                cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
                cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
                cfg.AddOpenBehavior(typeof(Caching.Behaviors.CachingBehavior<,>));
                cfg.AddOpenBehavior(typeof(Caching.Behaviors.InvalidateCachingBehavior<,>));
            });


            services.AddScoped<IMapper>(sp =>
            {
                var config = new MapperConfiguration(cfg =>
                {
                    cfg.ShouldMapMethod = _ => false;
                    cfg.AddProfile<ProductProfile>();
                });
                config.AssertConfigurationIsValid();
                return config.CreateMapper();
            });


            services.AddValidatorsFromAssembly(assembly);

            return services;
        }
    }
}
