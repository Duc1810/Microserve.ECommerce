using BuildingBlocks.Messaging;
using BuildingBlocks.Messaging.Events;
using Microsoft.Extensions.Configuration;
using System.Web;

namespace Authentication.Application.Features.Auth.ForgotPassword
{
    public class ForgotPasswordCommandHandler : ICommandHandler<ForgotPasswordCommand, Result<ForgotPasswordResult>>
    {
        private readonly UserManager<User> _userManager;
        private readonly ILogger<ForgotPasswordCommandHandler> _logger;
        private readonly IConfiguration _config;
        private readonly IEventBus _eventBus;

        public ForgotPasswordCommandHandler(
            UserManager<User> userManager,
            ILogger<ForgotPasswordCommandHandler> logger,
            IConfiguration config,
            IEventBus eventBus)
        {
            _userManager = userManager;
            _logger = logger;
            _config = config;
            _eventBus = eventBus;
        }

        public async Task<Result<ForgotPasswordResult>> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var logContext = new { Command = nameof(ForgotPasswordCommand), Email = request.Email };

                var user = await _userManager.FindByEmailAsync(request.Email.ToString());
                if (user == null)
                {
                    _logger.LogInformation($"[{nameof(Handle)}] forgot_password_nonexistent_email {logContext}");
                    return Result<ForgotPasswordResult>.ResponseSuccess(new ForgotPasswordResult(true));
                }

                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var tokenEncoded = HttpUtility.UrlEncode(token);

                var resetLink = BuildResetPasswordLink(user.Email!, tokenEncoded);

                await PublishForgotPasswordEventAsync(user.Email!, token, resetLink);

                return Result<ForgotPasswordResult>.ResponseSuccess(new ForgotPasswordResult(true));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[{nameof(Handle)}] unexpected_error");
                return Result<ForgotPasswordResult>.ResponseError(
                    code: ErrorCodes.InternalError,
                    message: ErrorMessages.InternalServerError,
                    status: HttpStatusCode.InternalServerError
                );
            }
        }

        private string BuildResetPasswordLink(string email, string tokenEncoded)
        {
            var baseUrl = _config["Auth:ResetPasswordUrl"] ?? "http://localhost:5151/reset-password";
            return $"{baseUrl}?token={tokenEncoded}&email={Uri.EscapeDataString(email)}";
        }

        private async Task PublishForgotPasswordEventAsync(string email, string token, string resetLink)
        {
            var registerEvent = new UserCreatedEvent
            {
                Email = email,
                Token = token,
                Title = "Đặt lại mật khẩu",
                Message = "Nhấn vào liên kết để đặt lại mật khẩu.",
                Href = resetLink
            };

            await _eventBus.PublishAsync(registerEvent, routingKeyName: "UserCreated");
        }
    }
}
