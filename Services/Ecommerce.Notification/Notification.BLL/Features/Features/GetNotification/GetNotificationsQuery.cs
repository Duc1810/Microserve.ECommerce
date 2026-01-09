using BuildingBlocks.Observability.Pagination;
namespace Notification.BLL.Features.Notifications.Queries;

public record GetNotificationsQuery(PaginationParam Params) : IQuery<Result<GetNotificationsResult>>;
public record GetNotificationsResult(PaginatedResult<NotificationDto> Lists);