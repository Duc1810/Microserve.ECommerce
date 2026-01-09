using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Notification.API.Hubs
{
    [Authorize(Policy = "ApiScope")]
    public class NotificationHub : Hub
    {

        public override Task OnConnectedAsync()
        {
            
            return base.OnConnectedAsync();
        }
    }
}
