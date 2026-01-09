
using BuildingBlocks.Results;

namespace Authentication.Application.Features.Auth.RevokeToken;

public sealed record RevokeTokenCommand(string RefreshToken): ICommand<Result<RevokeTokenResponse>>;

public sealed class RevokeTokenResponse
{
    public bool Revoked { get; init; }
}