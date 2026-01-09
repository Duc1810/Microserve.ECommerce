
namespace Authentication.Application.Features.Auth.ChangePassword;

public record ChangePasswordCommand(
    Guid UserId,
    string CurrentPassword,
    string NewPassword
) : ICommand<Result<ChangePasswordResult>>;

public record ChangePasswordResult(bool Succeeded);
