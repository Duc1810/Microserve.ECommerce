using Notification.BLL.Commons.MessageErros;
namespace Notification.BLL.Features.Features.CreateNotification;

public class CreateNotificationHandler
    : ICommandHandler<CreateNotificationCommand, Result<CreateNotificationResult>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationDispatcher _dispatcher;
    private readonly ILogger<CreateNotificationHandler> _logger;
    private readonly ICurrentUser _currentUser;

    public CreateNotificationHandler(
        IUnitOfWork unitOfWork,
        INotificationDispatcher dispatcher,
        ILogger<CreateNotificationHandler> logger, ICurrentUser currentUser)
    {
        _unitOfWork = unitOfWork;
        _dispatcher = dispatcher;
        _logger = logger;
        _currentUser = currentUser;
    }

    public async Task<Result<CreateNotificationResult>> Handle(CreateNotificationCommand command, CancellationToken cancellationToken)
    {
        try
        {
            var userId = _currentUser.UserId?.Trim();
            if (string.IsNullOrWhiteSpace(userId))
            {
                _logger.LogWarning("[CreateCart] unauthorized_no_userid");
                return Result<CreateNotificationResult>.ResponseError(ErrorCodes.Unauthorized, ErrorMessages.UnauthorizedAccess, HttpStatusCode.Unauthorized);
            }

            var notification = command.Adapt<Data.Notification>();
            notification.UserId = userId;
            notification.CreatedAt = DateTime.UtcNow;
            notification.IsRead = false;
            notification.ReadAt = null;
            notification.Type = NotificationType.USER;

            var notificationRepository = _unitOfWork.GetRepository<Data.Notification>();

            await notificationRepository.AddAsync(notification);

            await _unitOfWork.SaveAsync();

            var notificationDto = notification.Adapt<NotificationDto>();

            await _dispatcher.PushToUserAsync(notificationDto.UserId, notificationDto);

            var  notificationResult = new CreateNotificationResult(notification.Id, notification.UserId);

            return Result<CreateNotificationResult>.ResponseSuccess(notificationResult, Messages.NotificationCreatedSuccessfully);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CreateNotification] unexpected_error UserId={UserId}", _currentUser.UserId);

            return Result<CreateNotificationResult>.ResponseError(
                code: ErrorCodes.InternalError,
                message: ErrorMessages.InternalServerError,
                status: System.Net.HttpStatusCode.InternalServerError
            );
        }
    }
}
