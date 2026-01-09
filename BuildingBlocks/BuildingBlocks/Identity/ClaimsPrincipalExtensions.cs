
using System.Security.Claims;
namespace BuildingBlocks.Identity
{
    public static class ClaimsPrincipalExtensions
    {
        public static string? FindFirstValue(this ClaimsPrincipal user, string type)
            => user?.Claims?.FirstOrDefault(c => c.Type == type)?.Value;
    }
}
