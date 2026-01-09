using Authentication.Application.Dtos.Auth;

namespace Authentication.Application.Features.Auth.RefreshToken;

public class RefreshTokenCommandHandler : ICommandHandler<RefreshTokenCommand, Result<RefreshTokenResult>>
{
    private readonly ITokenService _tokenService;
    private readonly ILogger<RefreshTokenCommandHandler> _logger;

    public RefreshTokenCommandHandler(
        ITokenService tokenService,
        ILogger<RefreshTokenCommandHandler> logger)
    {
        _tokenService = tokenService;
        _logger = logger;
    }

    public async Task<Result<RefreshTokenResult>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var logContext = new { Command = nameof(RefreshTokenCommand) };

            var tokenResponse = await _tokenService.RefreshTokenAsync(request.RefreshToken, cancellationToken);

            if (tokenResponse.IsError)
            {
                _logger.LogError($"[{nameof(Handle)}] refresh_token_error {tokenResponse.Error} {tokenResponse.ErrorDescription} {logContext}");
                var  errorsToken = string.Join("; ", new[] { tokenResponse.Error, tokenResponse.ErrorDescription }.Where(e => !string.IsNullOrWhiteSpace(e)));
                return Result<RefreshTokenResult>.ResponseError(CodeStatus.UnableAccessToken,ErrorMessages.UnableAccessToken,HttpStatusCode.BadRequest, errorsToken);
            }

            var result = new RefreshTokenResult
            {
                AccessToken = tokenResponse.AccessToken!,
                RefreshToken = tokenResponse.RefreshToken,
                ExpiresIn = tokenResponse.ExpiresIn
            };

            return Result<RefreshTokenResult>.ResponseSuccess(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"[{nameof(Handle)}] unexpected_error");
            return Result<RefreshTokenResult>.ResponseError(
                code: ErrorCodes.InternalError,
                message: ErrorMessages.InternalServerError,
                status: HttpStatusCode.InternalServerError
            );
        }
    }
}

