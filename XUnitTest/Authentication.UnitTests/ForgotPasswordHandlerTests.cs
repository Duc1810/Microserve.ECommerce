using Authentication.Application.Features.Auth.ForgotPassword;
using Authentication.Domain.Entities;
using BuildingBlocks.Messaging;
using BuildingBlocks.Messaging.Events;
using Common.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace Authentication.UnitTests.Handlers;

public class ForgotPasswordHandlerTests
{
    private readonly Mock<UserManager<User>> _mockUserManager;
    private readonly Mock<ILogger<ForgotPasswordCommandHandler>> _mockLogger;
    private readonly Mock<IConfiguration> _mockConfig;
    private readonly Mock<IEventBus> _mockEventBus;
    private readonly ForgotPasswordCommandHandler _handler;

    public ForgotPasswordHandlerTests()
    {
        _mockUserManager = IdentityMockHelpers.CreateUserManagerMock<User>();
        _mockLogger = new Mock<ILogger<ForgotPasswordCommandHandler>>();
        _mockConfig = new Mock<IConfiguration>();
        _mockEventBus = new Mock<IEventBus>();

        _mockConfig.Setup(c => c["Auth:ResetPasswordUrl"]).Returns("https://frontend.app/reset-password");

        _handler = new ForgotPasswordCommandHandler(
            _mockUserManager.Object,
            _mockLogger.Object,
            _mockConfig.Object,
            _mockEventBus.Object);
    }

    [Fact]
    public async Task Handle_Should_Return_Success_And_Not_Expose_When_Email_Not_Found()
    {
        var cmd = new ForgotPasswordCommand("unknown@mail.com");

        _mockUserManager.Setup(m => m.FindByEmailAsync(cmd.Email))
                        .ReturnsAsync((User?)null);

        var result = await _handler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Succeeded.Should().BeTrue();

        _mockUserManager.Verify(m => m.GeneratePasswordResetTokenAsync(It.IsAny<User>()), Times.Never);
        _mockEventBus.Verify(m => m.PublishAsync(It.IsAny<UserCreatedEvent>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Should_Generate_Token_And_Publish_Event_When_User_Found()
    {
        var cmd = new ForgotPasswordCommand("alice@mail.com");
        var user = new User { Email = cmd.Email, UserName = "alice" };
        var token = "abc def+ghi==";
        UserCreatedEvent? dispatchedEvent = null;

        _mockUserManager.Setup(m => m.FindByEmailAsync(cmd.Email)).ReturnsAsync(user);
        _mockUserManager.Setup(m => m.GeneratePasswordResetTokenAsync(user)).ReturnsAsync(token);
        _mockEventBus.Setup(e => e.PublishAsync(It.IsAny<UserCreatedEvent>(), "UserCreated"))
                     .Callback<UserCreatedEvent, string>((e, _) => dispatchedEvent = e)
                     .Returns(Task.CompletedTask);

        var result = await _handler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Succeeded.Should().BeTrue();

        _mockUserManager.Verify(m => m.GeneratePasswordResetTokenAsync(user), Times.Once);
        _mockEventBus.Verify(m => m.PublishAsync(It.IsAny<UserCreatedEvent>(), "UserCreated"), Times.Once);

        dispatchedEvent.Should().NotBeNull();
        dispatchedEvent!.Email.Should().Be("alice@mail.com");
        dispatchedEvent.Title.Should().Contain("Đặt lại mật khẩu");
        dispatchedEvent.Href.Should().Contain("https://frontend.app/reset-password");
    }
}
