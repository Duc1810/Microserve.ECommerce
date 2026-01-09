

namespace Authentication.Application.Features.Auth.ForgotPassword;

public sealed record ForgotPasswordCommand(string Email)
    : ICommand<Result<ForgotPasswordResult>>;

public sealed record ForgotPasswordResult(bool Succeeded);
