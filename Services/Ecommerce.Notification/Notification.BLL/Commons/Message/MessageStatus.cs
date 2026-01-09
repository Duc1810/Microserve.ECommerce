

namespace Notification.BLL.Commons.MessageErros
{
    public static class StatusCodeErrors
    {
        public const string NotificationNotFound = "NOTIFICATION_NOT_FOUND";
        public const string Unauthorized = "UNAUTHORIZED";
        public const string Forbidden = "FORBIDDEN";
        public const string NotificationInternalError = "NOTIFICATION_INTERNAL_ERROR";
    }

    public static class Messages
    {
        public const string NotificationNotFound = "Notification not found.";
        public const string UnauthorizedAccess = "No user id in token.";
        public const string Forbidden = "You cannot modify this notification.";
        public const string InternalServerError = "An unexpected error occurred while processing notification.";
        public const string NotificationSentSuccessfully = "Notification sent successfully.";
        public const string NotificationCreatedSuccessfully = "Notification created successfully.";
    }
}
