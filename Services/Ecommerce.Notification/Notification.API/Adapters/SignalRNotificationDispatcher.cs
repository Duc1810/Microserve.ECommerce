using Microsoft.AspNetCore.SignalR;
using Notification.API.Hubs;
using Notification.BLL.Commons.Dtos;
using Notification.BLL.Commons.Ports;


public class SignalRNotificationDispatcher : INotificationDispatcher
{
    private readonly IHubContext<NotificationHub> _hub;
    public SignalRNotificationDispatcher(IHubContext<NotificationHub> hub) => _hub = hub;

    public Task PushToUserAsync(string userId, NotificationDto n, CancellationToken ct = default)
        => _hub.Clients.User(userId).SendAsync("notification:created", n, ct);
}

