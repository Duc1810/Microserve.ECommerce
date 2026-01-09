using System.Net;
using System.Text.Json;
using Authentication.Application.Features.Auth.RegisterUser;
using Authentication.Domain.Entities;
using BuildingBlocks.Messaging;
using BuildingBlocks.Messaging.Events;
using Common.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;


namespace Authentication.UnitTests.Handlers;

public class RegisterUserHandlerTests
{
    private readonly Mock<UserManager<User>> _mockUserManager;
    private readonly Mock<ILogger<RegisterUserCommandHandler>> _mockLogger;
    private readonly Mock<IEventBus> _mockEventBus;
    private readonly IDataProtectionProvider _dataProtection;
    private readonly RegisterUserCommandHandler _handler;

    public RegisterUserHandlerTests()
    {
        _mockUserManager = IdentityMockHelpers.CreateUserManagerMock<User>();
        _mockLogger = new Mock<ILogger<RegisterUserCommandHandler>>();
        _mockEventBus = new Mock<IEventBus>();
        _dataProtection = new EphemeralDataProtectionProvider();

        _handler = new RegisterUserCommandHandler(
            _mockUserManager.Object,
            _mockLogger.Object,
            _mockEventBus.Object,
            _dataProtection
        );
    }

    [Fact]
    public async Task Handle_Should_Return_Success_And_Publish_Confirmation_Email()
    {
        var request = new RegisterUserCommand( new RegisterUserRequest( "alice","alice@mail.com","P@ssw0rd!","P@ssw0rd!"));

        var registerCommand = new RegisterUserCommand(request.PayLoad);


        var expectedEmailToken = "abc def+ghi==";
        User? createdUser = null;
        UserCreatedEvent? dispatchedEvent = null;

        _mockUserManager.Setup(x => x.FindByEmailAsync(request.PayLoad.Email)).ReturnsAsync((User?)null);
        _mockUserManager.Setup(x => x.FindByNameAsync(request.PayLoad.UserName)).ReturnsAsync((User?)null);

        _mockUserManager
            .Setup(x => x.CreateAsync(It.IsAny<User>(), request.PayLoad.Password))
            .Callback<User, string>((user, _) =>
            {
                user.Id = "user-123"; 
                createdUser = user;
            })
            .ReturnsAsync(IdentityResult.Success);

        _mockUserManager
            .Setup(x => x.GenerateEmailConfirmationTokenAsync(It.IsAny<User>()))
            .ReturnsAsync(expectedEmailToken);

        _mockUserManager
            .Setup(x => x.AddToRoleAsync(It.IsAny<User>(), "User"))
            .ReturnsAsync(IdentityResult.Success);

        _mockEventBus
            .Setup(x => x.PublishAsync(It.IsAny<UserCreatedEvent>(), "UserCreated"))
            .Callback<UserCreatedEvent, string>((eventData, _) => dispatchedEvent = eventData)
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Message.Should().Contain("successfully");

        createdUser.Should().NotBeNull();
        createdUser!.UserName.Should().Be("alice");
        createdUser.Email.Should().Be("alice@mail.com");

        dispatchedEvent.Should().NotBeNull();
        dispatchedEvent!.Email.Should().Be("nguyenbaminhduc2019@gmail.com");
        dispatchedEvent.Title.Should().Be("Xác thực tài khoản");
        dispatchedEvent.Href.Should().Contain("/verify?token=");

        // Decode & validate protected token
        var uri = new Uri(dispatchedEvent.Href!);
        var tokenParam = System.Web.HttpUtility.ParseQueryString(uri.Query)["token"];
        tokenParam.Should().NotBeNullOrWhiteSpace();

        var protector = _dataProtection.CreateProtector("confirm-email:v1").ToTimeLimitedDataProtector();
        var decryptedJson = protector.Unprotect(System.Net.WebUtility.UrlDecode(tokenParam!));
        var decodedPayload = JsonSerializer.Deserialize<EmailConfirmPayload>(decryptedJson);

        decodedPayload.Should().NotBeNull();
        decodedPayload!.UserId.Should().Be("user-123");
        decodedPayload.Email.Should().Be("alice@mail.com");
        decodedPayload.Token.Should().Be(expectedEmailToken);
    }

    [Fact]
    public async Task Handle_Should_Return_Error_When_Username_Already_Exists()
    {
        // Arrange
        var request = new RegisterUserCommand(new RegisterUserRequest("alice", "a@mail.com", "123", "123"));

        _mockUserManager.Setup(x => x.FindByEmailAsync(request.PayLoad.Email)).ReturnsAsync((User?)null);
        _mockUserManager.Setup(x => x.FindByNameAsync(request.PayLoad.UserName)).ReturnsAsync(new User());

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error?.Code.Should().Be("auth.username_already_exists");
        result.Error?.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        _mockEventBus.Verify(e => e.PublishAsync(It.IsAny<UserCreatedEvent>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Should_Return_Error_When_Email_Already_Exists()
    {
        // Arrange
        var request = new RegisterUserCommand(new RegisterUserRequest("alice", "a@mail.com", "123", "123"));

        _mockUserManager.Setup(x => x.FindByEmailAsync(request.PayLoad.Email)).ReturnsAsync(new User());

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error?.Code.Should().Be("auth.email_already_exists");
        result.Error?.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        _mockEventBus.Verify(e => e.PublishAsync(It.IsAny<UserCreatedEvent>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Should_Return_Error_When_User_Creation_Fails()
    {
        // Arrange
        var request = new RegisterUserCommand(new RegisterUserRequest("alice", "a@mail.com", "123", "123"));


        _mockUserManager.Setup(x => x.FindByEmailAsync(request.PayLoad.Email)).ReturnsAsync((User?)null);
        _mockUserManager.Setup(x => x.FindByNameAsync(request.PayLoad.Password)).ReturnsAsync((User?)null);

        _mockUserManager
            .Setup(x => x.CreateAsync(It.IsAny<User>(), request.PayLoad.Password))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Password too weak" }));

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error?.Code.Should().Be("auth.user_creation_failed");
        result.Message.Should().Contain("Password too weak");
        result.Error?.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}

// Payload giống thực tế, dùng để deserialize token mã hóa
public sealed record EmailConfirmPayload(string UserId, string Email, string Token);
