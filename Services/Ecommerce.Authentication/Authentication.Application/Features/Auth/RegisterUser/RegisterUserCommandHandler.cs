
using Authentication.Domain.Enums;
using BuildingBlocks.Messaging;
using BuildingBlocks.Messaging.Events;

using Microsoft.AspNetCore.DataProtection;
using System.Text.Json;


namespace Authentication.Application.Features.Auth.RegisterUser;

public class RegisterUserCommandHandler : ICommandHandler<RegisterUserCommand, Result<RegisterUserResult>>
{
    private readonly UserManager<User> _userManager;
    private readonly ILogger<RegisterUserCommandHandler> _logger;
    private readonly IEventBus _eventBus;
    private readonly IDataProtectionProvider _dataProtection;
    private readonly string FrontendHref = "http://localhost:5151";

    public RegisterUserCommandHandler(
        UserManager<User> userManager,
        ILogger<RegisterUserCommandHandler> logger,
        IEventBus eventBus,
        IDataProtectionProvider dataProtection)
    {
        _userManager = userManager;
        _logger = logger;
        _eventBus = eventBus;
        _dataProtection = dataProtection;
    }

    public async Task<Result<RegisterUserResult>> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        try
        {

            var logContext = new { User = request.PayLoad.UserName, Email = request.PayLoad.Email };
            var payLoad = request.PayLoad;
            var normalizedEmail = payLoad.Email.Trim();
            var normalizedUser = payLoad.UserName.Trim();

            if (await _userManager.FindByEmailAsync(normalizedEmail) is not null)
            {
                _logger.LogWarning($"[{nameof(Handle)}] email_exists {logContext}");
                return Result<RegisterUserResult>.ResponseError(CodeStatus.EmailAlreadyExists, ErrorMessages.EmailAlreadyExists, HttpStatusCode.BadRequest);
            }

            if (await _userManager.FindByNameAsync(normalizedUser) is not null)
            {
                _logger.LogWarning($"[{nameof(Handle)}] username_exists {logContext}");
                return Result<RegisterUserResult>.ResponseError(CodeStatus.UsernameAlreadyExists, ErrorMessages.UsernameAlreadyExists, HttpStatusCode.BadRequest);
            }

            var user = new User
            {
                UserName = payLoad.UserName,
                Email = payLoad.Email,
                EmailConfirmed = false
            };

            var createUser = await _userManager.CreateAsync(user, payLoad.Password);

            if (!createUser.Succeeded)
            {
                var errors = string.Join(", ", createUser.Errors.Select(e => e.Description));
                _logger.LogError($"[{nameof(Handle)}] create_failed {errors} {logContext}");

                return Result<RegisterUserResult>.ResponseError(CodeStatus.UserCreationFailed, $"{ErrorMessages.UserCreationFailed}: {errors}", HttpStatusCode.BadRequest, createUser.Errors);
            }

            await _userManager.AddToRoleAsync(user, nameof(AuthRole.User));

            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);

            var confirmLink = await BuildConfirmLinkAsync(user, TimeSpan.FromHours(24), FrontendHref, token);

            await _eventBus.PublishAsync(new UserCreatedEvent
            {
                Email = "nguyenbaminhduc2019@gmail.com",
                Token = token,
                Title = "Xác thực tài khoản",
                Message = "Click link để xác thực",
                Href = confirmLink
            }, routingKeyName: "UserCreated");

            _logger.LogInformation($"[{nameof(Handle)}] success {logContext}");
            return Result<RegisterUserResult>.ResponseSuccess(new RegisterUserResult(Message: "User register successfully"));

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"[{nameof(Handle)}] unexpected_error");
            return Result<RegisterUserResult>.ResponseError(
                code: ErrorCodes.InternalError,
                message: ErrorMessages.InternalServerError,
                status: HttpStatusCode.InternalServerError
            );
        }
    }

    private async Task<string> BuildConfirmLinkAsync(User user, TimeSpan lifetime, string frontEndBaseUrl, string token)
    {
        var payload = new EmailConfirmPayload(user.Id, user.Email!, token);
        var json = JsonSerializer.Serialize(payload);

        var protector = _dataProtection.CreateProtector("confirm-email:v1");
        var timeProtector = protector.ToTimeLimitedDataProtector();
        var protectedPayload = timeProtector.Protect(json, lifetime);

        var tokenEncoded = WebUtility.UrlEncode(protectedPayload);
        return $"{frontEndBaseUrl}/verify?token={tokenEncoded}";
    }
}
