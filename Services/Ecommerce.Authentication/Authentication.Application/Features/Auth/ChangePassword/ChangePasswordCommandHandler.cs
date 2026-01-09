
using Authentication.Domain.Entities;

namespace Authentication.Application.Features.Auth.ChangePassword
{
    public class ChangePasswordCommandHandler : ICommandHandler<ChangePasswordCommand, Result<ChangePasswordResult>>
    {
        private readonly UserManager<User> _userManager;
        private readonly ILogger<ChangePasswordCommandHandler> _logger;

        public ChangePasswordCommandHandler(
            UserManager<User> userManager,
            ILogger<ChangePasswordCommandHandler> logger)
        {
            _userManager = userManager;
            _logger = logger;
        }

        public async Task<Result<ChangePasswordResult>> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var logContext = new { TargetUserId = request.UserId, Command = nameof(ChangePasswordCommand) };

                var user = await _userManager.FindByIdAsync(request.UserId.ToString());
                if (user is null)
                {
                    _logger.LogWarning($"[{nameof(Handle)}] not_found {logContext} ");
                    return Result<ChangePasswordResult>.ResponseError(CodeStatus.UserNotFound, Messages.UserNotFound, HttpStatusCode.NotFound);
                }

                var result = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);


                if (!result.Succeeded)
                {
                    var errors = string.Join("; ", result.Errors.Select(e => $"{e.Code}: {e.Description}"));
                    _logger.LogWarning($"[{nameof(Handle)}] change_password_failed errors:{errors} {logContext}");
                    return Result<ChangePasswordResult>.ResponseError(CodeStatus.ChangePasswordFailed, Messages.ChangePasswordFailed, HttpStatusCode.BadRequest, errors);
                }

                await _userManager.UpdateSecurityStampAsync(user);

                return Result<ChangePasswordResult>.ResponseSuccess(new ChangePasswordResult(true));

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[{Handler}.{Method}] unexpected_error userId={UserId}", nameof(ChangePasswordCommandHandler), nameof(Handle), request.UserId);

                return Result<ChangePasswordResult>.ResponseError(
                    code: ErrorCodes.InternalError,
                    message: ErrorMessages.InternalServerError,
                    status: HttpStatusCode.InternalServerError
                );
            }
        }
    }
}
