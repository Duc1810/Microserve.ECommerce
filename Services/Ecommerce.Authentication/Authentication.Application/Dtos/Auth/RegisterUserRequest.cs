namespace Authentication.Application.Features.Auth.RegisterUser;

public record RegisterUserRequest(
    string UserName,
    string Email,
    string Password,
    string ConfirmPassword
);

