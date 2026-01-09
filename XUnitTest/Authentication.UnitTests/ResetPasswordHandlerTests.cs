using Authentication.Application.Commons;
using Authentication.Application.Features.Auth.ResetPassword;
using Authentication.Domain.Entities;
using Common.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;
using System.Net;
using System.Web;

namespace Authentication.UnitTests.Handlers;

public class ResetPasswordHandlerTests
{
    private readonly Mock<UserManager<User>> _mockUserManager;
    private readonly Mock<ILogger<ResetPasswordCommandHandler>> _mockLogger;
    private readonly ResetPasswordCommandHandler _handler;

    public ResetPasswordHandlerTests()
    {
        _mockUserManager = IdentityMockHelpers.CreateUserManagerMock<User>();
        _mockLogger = new Mock<ILogger<ResetPasswordCommandHandler>>();
        _handler = new ResetPasswordCommandHandler(_mockUserManager.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task Handle_Should_Return_NotFound_When_User_Does_Not_Exist()
    {
        var cmd = new ResetPasswordCommand("nonexistent@mail.com", "encoded-token", "New123!");

        _mockUserManager.Setup(m => m.FindByEmailAsync(cmd.Email)).ReturnsAsync((User?)null);

        var result = await _handler.Handle(cmd, default);

        result.IsSuccess.Should().BeFalse();
        result.Error?.Code.Should().Be(CodeStatus.UserNotFound);
        result.Error?.StatusCode.Should().Be(HttpStatusCode.NotFound);

        _mockUserManager.Verify(m => m.ResetPasswordAsync(It.IsAny<User>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Should_Return_Error_When_Reset_Fails()
    {
        var plainToken = "reset-token";
        var encodedToken = HttpUtility.UrlEncode(plainToken);
        var cmd = new ResetPasswordCommand("alice@mail.com", encodedToken, "New123!");

        var user = new User { Email = cmd.Email };

        _mockUserManager.Setup(m => m.FindByEmailAsync(cmd.Email)).ReturnsAsync(user);
        _mockUserManager.Setup(m => m.ResetPasswordAsync(user, plainToken, cmd.NewPassword))
            .ReturnsAsync(IdentityResult.Failed(
                new IdentityError { Code = "InvalidToken", Description = "Token is invalid." },
                new IdentityError { Code = "TooShort", Description = "Password too short." }
            ));

        var result = await _handler.Handle(cmd, default);

        result.IsSuccess.Should().BeFalse();
        result.Error?.Code.Should().Be(CodeStatus.UserCreationFailed);
        result.Error?.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        _mockUserManager.Verify(m => m.UpdateSecurityStampAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Should_Update_SecurityStamp_When_Reset_Succeeds()
    {
        var plainToken = "valid-token";
        var encodedToken = HttpUtility.UrlEncode(plainToken);
        var cmd = new ResetPasswordCommand("alice@mail.com", encodedToken, "New123!");

        var user = new User { Email = cmd.Email };

        _mockUserManager.Setup(m => m.FindByEmailAsync(cmd.Email)).ReturnsAsync(user);
        _mockUserManager.Setup(m => m.ResetPasswordAsync(user, plainToken, cmd.NewPassword))
                        .ReturnsAsync(IdentityResult.Success);
        _mockUserManager.Setup(m => m.UpdateSecurityStampAsync(user)).ReturnsAsync(IdentityResult.Success);

        var result = await _handler.Handle(cmd, default);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Succeeded.Should().BeTrue();

        _mockUserManager.Verify(m => m.UpdateSecurityStampAsync(user), Times.Once);
    }
}
