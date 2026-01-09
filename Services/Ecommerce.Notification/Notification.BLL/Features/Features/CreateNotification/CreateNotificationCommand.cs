
namespace Notification.BLL.Features.Notifications;

public record CreateNotificationCommand(CreateNotificationRequest Request) : ICommand<Result<CreateNotificationResult>>; 

public record CreateNotificationResult(int Id, string UserId);
