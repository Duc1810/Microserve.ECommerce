using BuildingBlocks.Observability.BaseEntity;
using Notification.Data.Enums;


namespace Notification.Data
{
    public class Notification : Entity<int>
    {
        public string UserId { get; set; } = default!;     
        public string Title { get; set; } = string.Empty;   
        public string? TitleUnsign { get; set; }           
        public string Message { get; set; } = string.Empty;
        public string? Href { get; set; }                 
        public NotificationType Type { get; set; } = NotificationType.USER;

        public bool IsRead { get; set; } = false;
        public DateTime? ReadAt { get; set; }

        public string? Metadata { get; set; } 
    }
}
