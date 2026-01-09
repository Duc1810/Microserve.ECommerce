
namespace Authentication.Application.Features.Auth.LoginUser;
public class LoginUserCommandHandler : ICommandHandler<LoginUserCommand, Result<LoginUserResult>>
{
    private readonly SignInManager<User> _signInManager;
    private readonly UserManager<User> _userManager;
    private readonly ITokenService _tokenService;
    private readonly ILogger<LoginUserCommandHandler> _logger;
    public LoginUserCommandHandler(
        SignInManager<User> signInManager,
        UserManager<User> userManager,
        ITokenService tokenService,
        ILogger<LoginUserCommandHandler> logger)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _tokenService = tokenService;
        _logger = logger;
    }

    public async Task<Result<LoginUserResult>> Handle(LoginUserCommand command, CancellationToken cancellationToken)
    {

        try
        {
            var logContext = new { User = command.PayLoad.UserName, Command = nameof(LoginUserCommand) };
            var payload = command.PayLoad;

            var user = await _userManager.FindByNameAsync(payload.UserName);

            if (user is null)
            {
                _logger.LogWarning($"[{nameof(Handle)}] not_found {logContext} ");
                return Result<LoginUserResult>.ResponseError(CodeStatus.UserNotFound,ErrorMessages.UserNotFound,HttpStatusCode.NotFound);
            }


            var passwordCheck = await _signInManager.CheckPasswordSignInAsync(user, payload.Password, false);

            // Invalid credentials
            if (!passwordCheck.Succeeded)
            {
                _logger.LogWarning($"[{nameof(Handle)}] invalid_credentials with userId : {user.Id} ");
                return Result<LoginUserResult>.ResponseError(CodeStatus.InvalidCredentials, ErrorMessages.InvalidCredentials, HttpStatusCode.BadRequest);
            }

            // Request token from IdentityServer
            var tokenResponse = await _tokenService.RequestPasswordTokenAsync(payload.UserName, payload.Password, cancellationToken);

            if (tokenResponse.IsError)
            {
                _logger.LogError($"[{nameof(Handle)}] token_error {tokenResponse.Error} {tokenResponse.ErrorDescription} {logContext}");
                var errorToken = new { tokenResponse.Error, tokenResponse.ErrorDescription };
                return Result<LoginUserResult>.ResponseError(CodeStatus.UnableAccessToken, ErrorMessages.UnableAccessToken, HttpStatusCode.BadRequest, errorToken);
            }

            var roles = await _userManager.GetRolesAsync(user);

            var loginUserResult = new LoginUserResult
            {
                AccessToken = tokenResponse.AccessToken!,
                RefreshToken = tokenResponse.RefreshToken,
                UserId = user.Id,
                TokenType = tokenResponse.TokenType!,
                ExpiresIn = tokenResponse.ExpiresIn,
                Roles = roles.ToArray()
            };

            return Result<LoginUserResult>.ResponseSuccess(loginUserResult);

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"[{nameof(Handle)}] unexpected_error");
            return Result<LoginUserResult>.ResponseError(
                code: ErrorCodes.InternalError,
                message: ErrorMessages.InternalServerError,
                status: HttpStatusCode.InternalServerError
            );
        }

    }
}