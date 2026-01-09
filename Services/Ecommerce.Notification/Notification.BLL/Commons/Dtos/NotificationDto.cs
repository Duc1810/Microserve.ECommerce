
namespace Notification.BLL.Commons.Dtos
{
    public class NotificationDto
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? TitleUnsign { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? Href { get; set; } = "https://localhost:7045/";
        public int Type { get; set; } = 1;
        public bool IsRead { get; set; } = false;
        public DateTime? ReadAt { get; set; }
        public DateTime? CreatedAt { get; set; }
    }
    public record CreateNotificationRequest(
    string Title,
    string? TitleUnsign,
    string Message,
    string? Href,
    NotificationType Type,
    string? Metadata);

}
