using Notification.BLL.Commons.MessageErros;

namespace Notification.BLL.Features.Features.MaskRead
{
    public record MarkAsReadCommand(int Id) : ICommand<Result<MarkAsReadResult>>;
    public record MarkAsReadResult(int Id, bool AlreadyRead);

    public class MarkAsReadCommandValidator : AbstractValidator<MarkAsReadCommand>
    {
        public MarkAsReadCommandValidator()
        {
            RuleFor(x => x.Id)
                .GreaterThan(0).WithMessage("Id must be greater than 0.");
        }
    }

    public class MarkAsReadHandler
        : ICommandHandler<MarkAsReadCommand, Result<MarkAsReadResult>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUser _currentUser;
        private readonly ILogger<MarkAsReadHandler> _logger;

        public MarkAsReadHandler(
            IUnitOfWork unitOfWork,
            ICurrentUser currentUser,
            ILogger<MarkAsReadHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _currentUser = currentUser;
            _logger = logger;
        }

        public async Task<Result<MarkAsReadResult>> Handle(MarkAsReadCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var userId = _currentUser.UserId?.Trim();
                if (string.IsNullOrWhiteSpace(userId))
                {
                    _logger.LogWarning("[CreateCart] unauthorized_no_userId");
                    return Result<MarkAsReadResult>.ResponseError(ErrorCodes.Unauthorized, ErrorMessages.UnauthorizedAccess, HttpStatusCode.Unauthorized);
                }

                var notificationRepository = _unitOfWork.GetRepository<Data.Notification>();

                var notification = await notificationRepository.GetByPropertyAsync(n => n.Id == request.Id);

                if (notification is null)
                {
                    _logger.LogWarning("[MarkAsRead] not_found id={Id}", request.Id);
                    return Result<MarkAsReadResult>.ResponseError(StatusCodeErrors.NotificationNotFound, $"Notification not found: {request.Id}", HttpStatusCode.NotFound);
                }

                var alreadyRead = notification.IsRead;

                if (!alreadyRead)
                {
                    notification.IsRead = true;
                    notification.ReadAt = DateTime.UtcNow;

                    await notificationRepository.UpdateAsync(notification);
                    await _unitOfWork.SaveAsync();

                    _logger.LogInformation("[MarkAsRead] updated id={Id}", notification.Id);
                }
                else
                {
                    _logger.LogInformation("[MarkAsRead] no_change id={Id} (already read)", notification.Id);
                }

                var markAsReadResult = new MarkAsReadResult(notification.Id, alreadyRead);
                return Result<MarkAsReadResult>.ResponseSuccess(markAsReadResult, "Notification already read.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[MarkAllAsRead] unexpected_error UserId={UserId}", _currentUser.UserId);

                return Result<MarkAsReadResult>.ResponseError(
                    code: ErrorCodes.InternalError,
                    message: ErrorMessages.InternalServerError,
                    status: HttpStatusCode.InternalServerError
                );
            }
        }
    }
}
