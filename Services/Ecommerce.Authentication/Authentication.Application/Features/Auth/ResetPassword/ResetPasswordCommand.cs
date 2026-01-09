
using BuildingBlocks.Results;

namespace Authentication.Application.Features.Auth.ResetPassword;

public record ResetPasswordCommand(
    string Email,
    string Token,
    string NewPassword
) : ICommand<Result<ResetPasswordResult>>;

public record ResetPasswordResult(bool Succeeded);
