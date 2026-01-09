
namespace Notification.BLL.Features.Features.MaskRead
{
    public record MarkAllAsReadCommand() : ICommand<Result<MarkAllAsReadResult>>;
    public record MarkAllAsReadResult(string UserId, int AffectedCount);



    public class MarkAllAsReadHandler
        : ICommandHandler<MarkAllAsReadCommand, Result<MarkAllAsReadResult>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUser _currentUser;
        private readonly ILogger<MarkAllAsReadHandler> _logger;

        public MarkAllAsReadHandler(
            IUnitOfWork uow,
            ICurrentUser currentUser,
            ILogger<MarkAllAsReadHandler> logger)
        {
            _unitOfWork = uow;
            _currentUser = currentUser;
            _logger = logger;
        }

        public async Task<Result<MarkAllAsReadResult>> Handle(MarkAllAsReadCommand request, CancellationToken ct)
        {
            try
            {
                var userId = _currentUser.UserId?.Trim();
                if (string.IsNullOrWhiteSpace(userId))
                {
                    _logger.LogWarning("[CreateCart] unauthorized_no_username");
                    return Result<MarkAllAsReadResult>.ResponseError(ErrorCodes.Unauthorized, ErrorMessages.UnauthorizedAccess, HttpStatusCode.Unauthorized);
                }



                var notificationRepository = _unitOfWork.GetRepository<Data.Notification>();

                var (notificationUnRead, totalCount) = await notificationRepository.GetAllByPropertyWithCountAsync(
                    filter: n => n.UserId == userId && !n.IsRead,
                    pageNumber: 1,
                    pageSize: int.MaxValue
                );

                if (notificationUnRead is null || totalCount == 0)
                {
                    _logger.LogInformation("[MarkAllAsRead] nothing_to_update userId={UserId}", userId);
                    return Result<MarkAllAsReadResult>.ResponseSuccess(new MarkAllAsReadResult(userId, 0));
                }

                foreach (var notification in notificationUnRead)
                {
                    notification.IsRead = true;
                    notification.ReadAt = DateTime.UtcNow;
                }

                await notificationRepository.UpdateRangeAsync(notificationUnRead);
                await _unitOfWork.SaveAsync();

                _logger.LogInformation("[MarkAllAsRead] success userId={UserId} affected={Count}", userId, notificationUnRead.Count);

                return Result<MarkAllAsReadResult>.ResponseSuccess(new MarkAllAsReadResult(userId, notificationUnRead.Count));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[MarkAllAsRead] unexpected_error UserId={UserId}", _currentUser.UserId);

                return Result<MarkAllAsReadResult>.ResponseError(
                    code: ErrorCodes.InternalError,
                    message: ErrorMessages.InternalServerError,
                    status: System.Net.HttpStatusCode.InternalServerError
                );
            }
        }
    }
}
