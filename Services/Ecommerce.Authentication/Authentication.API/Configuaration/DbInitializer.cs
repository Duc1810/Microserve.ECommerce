using Authentication.Domain.Entities;
using Microsoft.AspNetCore.Identity;
namespace Authentication.API;

public static class DbInitializer
{
    public static async Task SeedAsync(IApplicationBuilder app)
    {
        using var scope = app.ApplicationServices.CreateScope();
        var roleMgr = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userMgr = scope.ServiceProvider.GetRequiredService<UserManager<User>>();


        var roles = new[] { "Admin", "User" };
        foreach (var r in roles)
        {
            if (!await roleMgr.RoleExistsAsync(r))
            {
                await roleMgr.CreateAsync(new IdentityRole(r));
            }
        }

    }
}
