
using Microsoft.EntityFrameworkCore;               
using Microsoft.Extensions.DependencyInjection;   

namespace Discount.API.Data 
{
    public static class Extensions
    {
        public static IApplicationBuilder UseMigration(this IApplicationBuilder app)
        {
            using var scope = app.ApplicationServices.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<DiscountContext>();

            dbContext.Database.Migrate(); // dùng bản sync, KHÔNG cần await

            return app;
        }
    }
}
