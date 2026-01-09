// Tests/Helpers/IdentityMockHelpers.cs
using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace Tests.Helpers;

public static class IdentityMockHelpers
{
    public static Mock<UserManager<TUser>> CreateUserManagerMock<TUser>() where TUser : class
    {
        var store = new Mock<IUserStore<TUser>>();
        var options = new Mock<IOptions<IdentityOptions>>();
        options.Setup(o => o.Value).Returns(new IdentityOptions());

        var mock = new Mock<UserManager<TUser>>(
            store.Object, options.Object, new PasswordHasher<TUser>(),
            Array.Empty<IUserValidator<TUser>>(),
            Array.Empty<IPasswordValidator<TUser>>(),
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            null, // services
            new Mock<ILogger<UserManager<TUser>>>().Object);

        return mock;
    }

    public static Mock<SignInManager<TUser>> CreateSignInManagerMock<TUser>(UserManager<TUser> userManager) where TUser : class
    {
        var contextAccessor = new Mock<IHttpContextAccessor>();
        var claimsFactory = new Mock<IUserClaimsPrincipalFactory<TUser>>();
        var options = new Mock<IOptions<IdentityOptions>>();
        options.Setup(o => o.Value).Returns(new IdentityOptions());

        var mock = new Mock<SignInManager<TUser>>(
            userManager, contextAccessor.Object, claimsFactory.Object,
            options.Object, new Mock<ILogger<SignInManager<TUser>>>().Object, null, null);

        return mock;
    }
}
