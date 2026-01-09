using Authentication.Domain.Entities;
using Duende.IdentityServer.Extensions;
using Duende.IdentityServer.Models;
using Duende.IdentityServer.Services;
using IdentityModel;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;


namespace Authentication.Infrastructure.Data.Configurations
{
    public class ProfileService(
    UserManager<User> _userManager,
    RoleManager<IdentityRole> _roleManager,
    IUserClaimsPrincipalFactory<User> _claimsFactory
) : IProfileService
    {

        public async Task GetProfileDataAsync(ProfileDataRequestContext context)
        {
            var sub = context.Subject.GetSubjectId();
            var user = await _userManager.FindByIdAsync(sub) ?? throw new KeyNotFoundException("User not found");
            var principal = await _claimsFactory.CreateAsync(user);

            var claims = principal.Claims.ToList();

            claims = claims.Where(claim => context.RequestedClaimTypes.Contains(claim.Type)).ToList();

            claims.Add(new Claim(JwtClaimTypes.Subject, sub));
            claims.Add(new Claim(JwtClaimTypes.Email, user?.Email ?? string.Empty));
            claims.Add(new Claim(JwtClaimTypes.Name, user?.UserName ?? string.Empty));
            claims.Add(new Claim(JwtClaimTypes.ClientId, context.Client?.ClientId ?? string.Empty));

            if (_userManager.SupportsUserRole)
            {
                var roles = await _userManager.GetRolesAsync(user); 
                foreach (var role in roles)
                {
                   
                    claims.Add(new Claim("role", role));                 
                    claims.Add(new Claim(ClaimTypes.Role, role));      

                    if (_roleManager.SupportsRoleClaims)
                    {
                        var roleEntity = await _roleManager.FindByNameAsync(role);
                        if (roleEntity != null)
                        {
                            var roleClaims = await _roleManager.GetClaimsAsync(roleEntity);
                            claims.AddRange(roleClaims);
                        }
                    }
                }
            }

            context.IssuedClaims = claims;
        }

        public async Task IsActiveAsync(IsActiveContext context)
        {
            var sub = context.Subject.GetSubjectId();
            var user = await _userManager.FindByIdAsync(sub);
            context.IsActive = user != null;
        }
    }
}
