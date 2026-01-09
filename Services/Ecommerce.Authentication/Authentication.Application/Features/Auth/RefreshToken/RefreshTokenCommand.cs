using Authentication.Application.Dtos.Auth;
using BuildingBlocks.Results;

namespace Authentication.Application.Features.Auth.RefreshToken;

public record RefreshTokenCommand(string RefreshToken): ICommand<Result<RefreshTokenResult>>;

