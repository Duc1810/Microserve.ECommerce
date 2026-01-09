namespace Notification.BLL.Commons.Dtos;

public class CreateNotificationDto
{
    public string Title { get; set; } = default!;
    public string? TitleUnsign { get; set; }
    public string Message { get; set; } = default!;
    public string? Href { get; set; }
    public NotificationType Type { get; set; }
    public string? Metadata { get; set; }
}
