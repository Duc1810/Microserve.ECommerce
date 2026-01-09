

namespace Authentication.Application.Dtos.Auth;
public record class RefreshTokenResult
{
    public string AccessToken { get; init; } = default!;
    public string? RefreshToken { get; init; }
    public string TokenType { get; init; } = "Bearer";
    public int ExpiresIn { get; init; }
}
