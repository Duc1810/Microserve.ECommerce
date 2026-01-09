


using Notification.BLL.Commons.Dtos;

namespace Notification.BLL.Commons.Ports
{
    public interface INotificationDispatcher
    {
        Task PushToUserAsync(string userId, NotificationDto notification, CancellationToken ct = default);
    }
}
