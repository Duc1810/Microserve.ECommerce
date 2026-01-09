

namespace Authentication.Application.Features.Auth.ResetPassword;

public class ResetPasswordCommandHandler : ICommandHandler<ResetPasswordCommand, Result<ResetPasswordResult>>
{
    private readonly UserManager<User> _userManager;
    private readonly ILogger<ResetPasswordCommandHandler> _logger;

    public ResetPasswordCommandHandler(UserManager<User> userManager, ILogger<ResetPasswordCommandHandler> logger)
    {
        _userManager = userManager;
        _logger = logger;
    }

    public async Task<Result<ResetPasswordResult>> Handle(ResetPasswordCommand command, CancellationToken ct)
    {
        try
        {
            var user = await _userManager.FindByEmailAsync(command.Email);
            if (user is null)
            {
                _logger.LogWarning($"[{nameof(ResetPasswordCommandHandler)}] user_not_found {command.Email}");
                return Result<ResetPasswordResult>.ResponseError(CodeStatus.UserNotFound, ErrorMessages.UserNotFound, HttpStatusCode.NotFound);
            }

            var token = System.Web.HttpUtility.UrlDecode(command.Token);
            var result = await _userManager.ResetPasswordAsync(user, token, command.NewPassword);

            if (!result.Succeeded)
            {
                var errors = string.Join("; ", result.Errors.Select(e => $"{e.Code}: {e.Description}"));
                _logger.LogError($"[{nameof(ResetPasswordCommandHandler)}] reset_failed {errors} with userId: {user.Id}");

                return Result<ResetPasswordResult>.ResponseError(CodeStatus.UserCreationFailed, ErrorMessages.UserCreationFailed, HttpStatusCode.BadRequest, result.Errors);
            }

            await _userManager.UpdateSecurityStampAsync(user);


            return Result<ResetPasswordResult>.ResponseSuccess(new ResetPasswordResult(true));

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"[{nameof(Handle)}] unexpected_error");
            return Result<ResetPasswordResult>.ResponseError(
                code: ErrorCodes.InternalError,
                message: ErrorMessages.InternalServerError,
                status: HttpStatusCode.InternalServerError
            );
        }
    }
}
