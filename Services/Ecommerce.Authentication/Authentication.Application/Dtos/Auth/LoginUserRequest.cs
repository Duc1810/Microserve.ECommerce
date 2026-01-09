namespace Authentication.Application.Dtos.Auth;

public sealed record LoginUserRequest
{
    public string UserName { get; init; } = default!;
    public string Password { get; init; } = default!;
}
