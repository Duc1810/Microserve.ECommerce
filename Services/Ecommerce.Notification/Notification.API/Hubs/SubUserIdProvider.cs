using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;

public class SubUserIdProvider : IUserIdProvider
{
    public string? GetUserId(HubConnectionContext connection)
        => connection.User?.FindFirst("sub")?.Value
           ?? connection.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
}