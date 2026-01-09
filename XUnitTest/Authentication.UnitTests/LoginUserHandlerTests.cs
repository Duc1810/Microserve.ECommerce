using Authentication.Application.Abstractions;
using Authentication.Application.Dtos.Auth;
using Authentication.Application.Features.Auth.LoginUser;
using Authentication.Domain.Entities;
using Common.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;

namespace Authentication.UnitTests.Handlers;

public class LoginUserHandlerTests
{
    private readonly Mock<UserManager<User>> _mockUserManager;
    private readonly Mock<SignInManager<User>> _mockSignInManager;
    private readonly Mock<ITokenService> _mockTokenService;
    private readonly Mock<ILogger<LoginUserCommandHandler>> _mockLogger;
    private readonly LoginUserCommandHandler _handler;

    public LoginUserHandlerTests()
    {
        _mockUserManager = IdentityMockHelpers.CreateUserManagerMock<User>();
        _mockSignInManager = IdentityMockHelpers.CreateSignInManagerMock(_mockUserManager.Object);
        _mockTokenService = new Mock<ITokenService>();
        _mockLogger = new Mock<ILogger<LoginUserCommandHandler>>();

        _handler = new LoginUserCommandHandler(
            _mockSignInManager.Object,
            _mockUserManager.Object,
            _mockTokenService.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task Handle_Should_Return_Success_When_Valid_Credentials()
    {
        // Arrange
        var payload = new LoginUserRequest
        {
            UserName = "alice",
            Password = "P@ssw0rd!"
        };
        var command = new LoginUserCommand(payload);
        var user = new User { Id = "user-1", UserName = "alice" };

        _mockUserManager.Setup(m => m.FindByNameAsync(payload.UserName)).ReturnsAsync(user);
        _mockSignInManager.Setup(m => m.CheckPasswordSignInAsync(user, payload.Password, false))
                          .ReturnsAsync(SignInResult.Success);
        _mockUserManager.Setup(m => m.GetRolesAsync(user))
                        .ReturnsAsync(new[] { "User", "Admin" });

        var tokenResponse = await TokenResponseFactory.OkAsync();

        _mockTokenService.Setup(s => s.RequestPasswordTokenAsync(payload.UserName, payload.Password, It.IsAny<CancellationToken>()))
                         .ReturnsAsync(tokenResponse);

        // Act
        var result = await _handler.Handle(command, default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.UserId.Should().Be("user-1");
        result.Value.AccessToken.Should().Be("access");
        result.Value.RefreshToken.Should().Be("refresh");
        result.Value.TokenType.Should().Be("Bearer");
        result.Value.ExpiresIn.Should().Be(3600);
        result.Value.Roles.Should().BeEquivalentTo("User", "Admin");
    }

    [Fact]
    public async Task Handle_Should_Return_Error_When_User_Not_Found()
    {
        // Arrange
        var payload = new LoginUserRequest
        {
            UserName = "bob",
            Password = "invalid"
        };
        var command = new LoginUserCommand(payload);

        _mockUserManager.Setup(m => m.FindByNameAsync(payload.UserName))
                        .ReturnsAsync((User?)null);

        // Act
        var result = await _handler.Handle(command, default);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull();
        result.Error?.Code.Should().Be("auth.user_not_found");
        result.Message.ToLower().Should().Contain("not found");
    }

    [Fact]
    public async Task Handle_Should_Return_Error_When_Password_Invalid()
    {
        // Arrange
        var payload = new LoginUserRequest
        {
            UserName = "alice",
            Password = "wrong-pass"
        };
        var command = new LoginUserCommand(payload);
        var user = new User { Id = "user-1", UserName = "alice" };

        _mockUserManager.Setup(m => m.FindByNameAsync(payload.UserName)).ReturnsAsync(user);
        _mockSignInManager.Setup(m => m.CheckPasswordSignInAsync(user, payload.Password, false))
                          .ReturnsAsync(SignInResult.Failed);

        // Act
        var result = await _handler.Handle(command, default);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error?.Code.Should().Be("auth.invalid_credentials");
        result.Message.ToLower().Should().Contain("invalid");
    }

    [Fact]
    public async Task Handle_Should_Return_Error_When_TokenService_Fails()
    {
        // Arrange
        var payload = new LoginUserRequest
        {
            UserName = "alice",
            Password = "P@ssw0rd!"
        };
        var command = new LoginUserCommand(payload);
        var user = new User { Id = "user-1", UserName = "alice" };

        _mockUserManager.Setup(m => m.FindByNameAsync(payload.UserName)).ReturnsAsync(user);
        _mockSignInManager.Setup(m => m.CheckPasswordSignInAsync(user, payload.Password, false))
                          .ReturnsAsync(SignInResult.Success);

        var tokenError = await TokenResponseFactory.ErrorAsync(
            error: "invalid_client",
            errorDescription: "Bad secret");

        _mockTokenService.Setup(s => s.RequestPasswordTokenAsync(payload.UserName, payload.Password, It.IsAny<CancellationToken>()))
                         .ReturnsAsync(tokenError);

        // Act
        var result = await _handler.Handle(command, default);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error?.Code.Should().Be("auth.unable_access_token");
        result.Message.ToLower().Should().Contain("unable");
        result.Error?.Details.Should().NotBeNull();
    }
}
