

using Authentication.Application.Features.Auth.ResetPassword;

namespace Authentication.Application.Features.Auth.RevokeToken;

public sealed class RevokeTokenCommandHandler
    : ICommandHandler<RevokeTokenCommand, Result<RevokeTokenResponse>>
{
    private readonly ITokenService _tokenService;
    private readonly ILogger<RevokeTokenCommandHandler> _logger;

    public RevokeTokenCommandHandler(
        ITokenService tokenService,
        ILogger<RevokeTokenCommandHandler> logger)
    {
        _tokenService = tokenService;
        _logger = logger;
    }

    public async Task<Result<RevokeTokenResponse>> Handle(RevokeTokenCommand request, CancellationToken cancellationToken)
    {

        try
        {

            var revokeResult = await _tokenService.RevokeTokenAsync(request.RefreshToken, cancellationToken);

            if (revokeResult.IsError)
            {
                _logger.LogError($"[{nameof(Handle)}] revoke_token_error {revokeResult.Error}");

                return Result<RevokeTokenResponse>.ResponseError(
                    code: "",
                    message: "",
                    status: HttpStatusCode.BadRequest,
                    details: new { revokeResult.Error }
                );
            }


            return Result<RevokeTokenResponse>.ResponseSuccess(new RevokeTokenResponse { Revoked = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"[{nameof(Handle)}] unexpected_error");
            return Result<RevokeTokenResponse>.ResponseError(
                code: ErrorCodes.InternalError,
                message: ErrorMessages.InternalServerError,
                status: HttpStatusCode.InternalServerError
            );
        }
    }
}
