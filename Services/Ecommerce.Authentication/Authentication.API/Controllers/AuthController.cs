using Authentication.Application.Abstractions;
using Authentication.Application.Dtos.Auth;
using Authentication.Application.Features.Auth.ChangePassword;
using Authentication.Application.Features.Auth.ConfirmEmail;
using Authentication.Application.Features.Auth.ForgotPassword;
using Authentication.Application.Features.Auth.LoginUser;
using Authentication.Application.Features.Auth.RefreshToken;
using Authentication.Application.Features.Auth.RegisterUser;
using Authentication.Application.Features.Auth.ResetPassword;
using Authentication.Application.Features.Auth.RevokeToken;
using BuildingBlocks.Observability.ApiResponse;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Authentication.API.Controllers
{
    [ApiController]
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiVersion("1.0")]

    public class AuthController : ControllerBase
    {
        private readonly ISender _sender;
        private readonly ILogoutService _logoutService;
        public AuthController(ISender sender, ILogoutService logoutService)
        {
            _sender = sender;
            _logoutService = logoutService;
        }


        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterUserRequest request)
        {
            var result = await _sender.Send(new RegisterUserCommand(request));
            return result.ToActionResult();
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginUserRequest request)
        {
            var result = await _sender.Send(new LoginUserCommand(request));
            return result.ToActionResult();
        }


        [HttpPost("refresh")]
        [AllowAnonymous]
        public async Task<IActionResult> Refresh([FromBody] RefreshTokenCommand request, CancellationToken ct)
        {
            var result = await _sender.Send(request, ct);
            return result.ToActionResult();
        }

        [HttpPost("revoke")]

        public async Task<IActionResult> Revoke([FromBody] RevokeTokenCommand request, CancellationToken ct)
        {
            var result = await _sender.Send(request, ct);
            return result.ToActionResult();
        }



        [HttpPost("change-password")]
        //[Authorize(Policy = "ApiScope")]

        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordCommand request, CancellationToken ct)
        {
            var result = await _sender.Send(request, ct);
            return result.ToActionResult();
        }


        [HttpPost("forgot-password")]
        [AllowAnonymous]

        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordCommand request, CancellationToken ct)
        {
            var result = await _sender.Send(request, ct);
            return result.ToActionResult();
        }

        [HttpPost("reset-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordCommand request, CancellationToken ct)
        {
            var result = await _sender.Send(request, ct);
            return result.ToActionResult();
        }

        [HttpPost("confirm-email")]
        [AllowAnonymous]
        public async Task<IActionResult> ConfirmEmail([FromBody] ConfirmEmailCommand request, CancellationToken ct)
        {
            var result = await _sender.Send(request, ct);
            return result.ToActionResult();
        }
        //[Authorize(Policy = "ApiScope")]
        [HttpPost("logout")]

        public async Task<IActionResult> Logout([FromBody] string refeshToken, CancellationToken ct)
        {
            await _logoutService.LogoutAsync(refeshToken, ct);
            return Ok("Logout successful");
        }

    }
}
