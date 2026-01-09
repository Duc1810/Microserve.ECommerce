using IdentityModel.Client;

namespace Authentication.Application.Abstractions;

public interface ITokenService
{
    Task<TokenResponse> RequestPasswordTokenAsync(string username, string password, CancellationToken cancellationToken);
    Task<TokenResponse> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken);
    Task<TokenRevocationResponse> RevokeTokenAsync(string refrshToken, CancellationToken cancellationToken);
}