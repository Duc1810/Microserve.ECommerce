
using System.Security.Claims;
namespace BuildingBlocks.Identity
{
    public interface ICurrentUser
    {
        string? UserId { get; }
        string? UserName { get; }
        string? Email { get; }
        string[] Roles { get; }
        bool IsAuthenticated { get; }
        ClaimsPrincipal Principal { get; }
    }
}
