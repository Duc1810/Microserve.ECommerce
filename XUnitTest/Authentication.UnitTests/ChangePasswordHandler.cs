using Authentication.Application.Features.Auth.ChangePassword;
using Authentication.Domain.Entities;
using Common.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;
using System.Net;



namespace Authentication.UnitTests.Handlers;

public class ChangePasswordHandlerTests
{
    private readonly Mock<UserManager<User>> _mockUserManager;
    private readonly Mock<ILogger<ChangePasswordCommandHandler>> _mockLogger;
    private readonly ChangePasswordCommandHandler _handler;

    public ChangePasswordHandlerTests()
    {
        _mockUserManager = IdentityMockHelpers.CreateUserManagerMock<User>();
        _mockLogger = new Mock<ILogger<ChangePasswordCommandHandler>>();

        _handler = new ChangePasswordCommandHandler(
            _mockUserManager.Object,
            _mockLogger.Object
        );
    }

    [Fact]
    public async Task Handle_Should_Return_Error_When_User_Not_Found()
    {
        var cmd = new ChangePasswordCommand(Guid.NewGuid(), "Old123!", "New123!");

        _mockUserManager.Setup(m => m.FindByIdAsync(cmd.UserId.ToString()))
                        .ReturnsAsync((User?)null);

        var result = await _handler.Handle(cmd, default);

        result.IsSuccess.Should().BeFalse();
        result.Error?.Code.Should().Be("auth.user_not_found");
        result.Error?.StatusCode.Should().Be(HttpStatusCode.NotFound);

        _mockUserManager.Verify(m => m.ChangePasswordAsync(It.IsAny<User>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Should_Return_Error_When_ChangePassword_Fails()
    {
        var cmd = new ChangePasswordCommand(Guid.NewGuid(), "Old123!", "weak");
        var user = new User { Id = cmd.UserId.ToString(), UserName = "alice" };

        _mockUserManager.Setup(m => m.FindByIdAsync(cmd.UserId.ToString())).ReturnsAsync(user);

        var errors = new[]
        {
            new IdentityError { Code = "PasswordTooShort", Description = "Must be at least 6 chars." },
            new IdentityError { Code = "PasswordRequiresDigit", Description = "Must have one digit." }
        };

        _mockUserManager.Setup(m => m.ChangePasswordAsync(user, cmd.CurrentPassword, cmd.NewPassword))
                        .ReturnsAsync(IdentityResult.Failed(errors));

        var result = await _handler.Handle(cmd, default);

        result.IsSuccess.Should().BeFalse();
        result.Error?.Code.Should().Be("auth.change_password_failed");
        result.Error?.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        result.Error?.Details.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_Should_Return_Success_When_ChangePassword_Succeeds()
    {
        // Arrange
        var cmd = new ChangePasswordCommand(Guid.NewGuid(), "Old123!", "New123!");
        var user = new User { Id = cmd.UserId.ToString(), UserName = "alice" };

        _mockUserManager.Setup(m => m.FindByIdAsync(cmd.UserId.ToString()))
            .ReturnsAsync(user);

        _mockUserManager.Setup(m => m.ChangePasswordAsync(user, cmd.CurrentPassword, cmd.NewPassword))
            .ReturnsAsync(IdentityResult.Success);

        _mockUserManager.Setup(m => m.UpdateSecurityStampAsync(user))
     .ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _handler.Handle(cmd, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Succeeded.Should().BeTrue();

        _mockUserManager.Verify(m => m.UpdateSecurityStampAsync(user), Times.Once);
    }


}
