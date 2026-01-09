

namespace Authentication.Application.Features.Auth.ConfirmEmail;

public sealed record ConfirmEmailCommand(string Token) : ICommand<Result<ConfirmEmailResult>>;

public sealed record ConfirmEmailResult(bool Succeeded);


