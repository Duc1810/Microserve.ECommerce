
namespace Authentication.Application.Features.Auth.RegisterUser;

public record RegisterUserCommand(RegisterUserRequest PayLoad) : ICommand<Result<RegisterUserResult>>;

public record RegisterUserResult(string Message);
public record EmailConfirmPayload(string UserId, string Email, string Token);