using System.Linq.Expressions;
using BuildingBlocks.Observability.Pagination;
using Notification.BLL.Commons.MessageErros;

namespace Notification.BLL.Features.Notifications.Queries;

public class GetNotificationsHandler
    : IQueryHandler<GetNotificationsQuery, Result<GetNotificationsResult>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GetNotificationsHandler> _logger;
    private readonly ICurrentUser _currentUser;

    public GetNotificationsHandler(
        IUnitOfWork unitOfWork,
        ILogger<GetNotificationsHandler> logger,
        ICurrentUser currentUser)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _currentUser = currentUser;
    }

    public async Task<Result<GetNotificationsResult>> Handle(GetNotificationsQuery command, CancellationToken ct)
    {
        try
        {
            var payload = command.Params;

            var userId = _currentUser.UserId?.Trim();
            if (string.IsNullOrWhiteSpace(userId))
            {
                _logger.LogWarning("[CreateCart] unauthorized_no_username");
                return Result<GetNotificationsResult>.ResponseError(ErrorCodes.Unauthorized,ErrorMessages.UnauthorizedAccess,HttpStatusCode.Unauthorized);
            }

            Expression<Func<Data.Notification, bool>> filter =
                n => n.UserId == userId;

            Expression<Func<Data.Notification, object>> orderBy =
                n => n.CreatedAt!;

            var (items, totalCount) = await _unitOfWork
                .GetRepository<Data.Notification>()
                .GetAllByPropertyWithCountAsync(
                    pageNumber: payload.PageNumber,
                    pageSize: payload.PageSize,
                    filter: filter,
                    orderBy: orderBy,
                    ascending: false
                );

            if (items == null || totalCount == 0)
            {
                _logger.LogWarning("[GetNotifications] no_notifications_found for userId={UserId}", _currentUser.UserId);
                return Result<GetNotificationsResult>.ResponseError(StatusCodeErrors.NotificationNotFound, Messages.NotificationNotFound, HttpStatusCode.NotFound);
            }

            var dtoItems = items.Adapt<List<NotificationDto>>();

            var paginated = new PaginatedResult<NotificationDto>(
                pageIndex: payload.PageNumber,
                pageSize: payload.PageSize,
                count: totalCount,
                data: dtoItems
            );

            return Result<GetNotificationsResult>.ResponseSuccess(new GetNotificationsResult(paginated), Messages.NotificationSentSuccessfully);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[GetNotifications] unexpected_error for userId={UserId}", _currentUser.UserId);

            return Result<GetNotificationsResult>.ResponseError(
                code: ErrorCodes.InternalError,
                message: ErrorMessages.InternalServerError,
                status: System.Net.HttpStatusCode.InternalServerError
            );
        }
    }
}
