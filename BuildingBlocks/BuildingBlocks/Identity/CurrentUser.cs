
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace BuildingBlocks.Identity;

public sealed class CurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    private readonly ClaimsPrincipal _user = accessor.HttpContext?.User ?? new ClaimsPrincipal();

    public ClaimsPrincipal Principal => _user;
    public bool IsAuthenticated => _user.Identity?.IsAuthenticated ?? false;

    public string? UserId => _user.FindFirstValue("sub") ?? _user.FindFirstValue(ClaimTypes.NameIdentifier);
    public string? Email => _user.FindFirstValue("email") ?? _user.FindFirstValue(ClaimTypes.Email);
    public string? UserName => _user.FindFirstValue("name") ?? _user.Identity?.Name;

    public string[] Roles => _user.FindAll("role")
                                  .Select(c => c.Value)
                                  .Concat(_user.FindAll(ClaimTypes.Role).Select(c => c.Value))
                                  .Distinct()
                                  .ToArray();
}
