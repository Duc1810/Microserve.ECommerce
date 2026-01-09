
using Authentication.Application.Dtos.Auth;
using BuildingBlocks.Results;

namespace Authentication.Application.Features.Auth.LoginUser;


public record LoginUserCommand(LoginUserRequest PayLoad): ICommand<Result<LoginUserResult>>;

public record LoginUserResult
{
    public string AccessToken { get; init; } = default!;
    public string? RefreshToken { get; init; }
    public string UserId { get; init; } = default!;
    public string TokenType { get; init; } = "Bearer";
    public int ExpiresIn { get; init; }
    public string[]? Roles { get; init; }
}