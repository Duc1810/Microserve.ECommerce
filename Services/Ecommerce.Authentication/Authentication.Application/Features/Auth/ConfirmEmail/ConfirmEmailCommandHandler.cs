using Authentication.Application.Features.Auth.RegisterUser;
using Microsoft.AspNetCore.DataProtection;
using System.Text.Json;


namespace Authentication.Application.Features.Auth.ConfirmEmail
{




    public class ConfirmEmailCommandHandler : ICommandHandler<ConfirmEmailCommand, Result<ConfirmEmailResult>>
    {
        private readonly UserManager<User> _userManager;
        private readonly IDataProtectionProvider _dataProtection;
        private readonly ILogger<ConfirmEmailCommandHandler> _logger;
        public ConfirmEmailCommandHandler(UserManager<User> userManager, IDataProtectionProvider dataProtection, ILogger<ConfirmEmailCommandHandler> logger)
        {
            _userManager = userManager;
            _dataProtection = dataProtection;
            _logger = logger;
        }

        public async Task<Result<ConfirmEmailResult>> Handle(ConfirmEmailCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var logContext = new { Command = nameof(ConfirmEmailCommand) };

                var raw = WebUtility.UrlDecode(request.Token)?.Replace(' ', '+');
                if (string.IsNullOrWhiteSpace(raw))
                {
                    _logger.LogWarning($"[{nameof(Handle)}] invalid_link (empty) {logContext}");
                    return Result<ConfirmEmailResult>.ResponseError(
                        code: CodeStatus.InvalidLink,
                        message: Messages.InvalidConfirmationLink,
                        status: HttpStatusCode.BadRequest
                    );
                }


                EmailConfirmPayload? payload = null;
                var protector = _dataProtection.CreateProtector("confirm-email:v1");
                var timeProtector = protector.ToTimeLimitedDataProtector();
                var json = timeProtector.Unprotect(raw);
                payload = JsonSerializer.Deserialize<EmailConfirmPayload>(json);

                if (payload is null)
                {
                    _logger.LogWarning($"[{nameof(Handle)}] invalid_or_expired_link {logContext}");
                    return Result<ConfirmEmailResult>.ResponseError(
                        code: CodeStatus.InvalidLink,
                        message: Messages.InvalidOrExpiredLink,
                        status: HttpStatusCode.BadRequest
                    );
                }


                var user = await _userManager.FindByIdAsync(payload.UserId)
                           ?? await _userManager.FindByEmailAsync(payload.Email);

                if (user is null)
                {
                    _logger.LogWarning($"[{nameof(Handle)}] user_not_found userId:{payload.UserId} email:{payload.Email} {logContext}");
                    return Result<ConfirmEmailResult>.ResponseError(CodeStatus.UserNotFound, Messages.UserNotFound, HttpStatusCode.NotFound);
                }

                if (user.EmailConfirmed)
                {
                    _logger.LogInformation($"[{nameof(Handle)}] already_confirmed userId:{user.Id} {logContext}");
                    return Result<ConfirmEmailResult>.ResponseSuccess(new ConfirmEmailResult(true));
                }

                var token = payload.Token?.Replace(' ', '+');
                var confirmResult = await _userManager.ConfirmEmailAsync(user, token!);

                if (!confirmResult.Succeeded)
                {
                    var messageErrors = string.Join("; ", confirmResult.Errors.Select(e => e.Description));
                    _logger.LogWarning($"[{nameof(Handle)}] confirm_failed userId:{user.Id} errors:{messageErrors} {logContext}");
                    return Result<ConfirmEmailResult>.ResponseError(CodeStatus.ConfirmEmailFailed, Messages.ConfirmEmailFailed, HttpStatusCode.BadRequest, messageErrors);
                }

                return Result<ConfirmEmailResult>.ResponseSuccess(new ConfirmEmailResult(true));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[{nameof(Handle)}] unexpected_error");
                return Result<ConfirmEmailResult>.ResponseError(
                    code: ErrorCodes.InternalError,
                    message: ErrorMessages.InternalServerError,
                    status: HttpStatusCode.InternalServerError
                );
            }
        }
    }
}
