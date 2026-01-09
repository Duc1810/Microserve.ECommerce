using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace Common.Helpers;

public static class IdentityMockHelpers
{
    public static Mock<UserManager<TUser>> CreateUserManagerMock<TUser>() where TUser : class
    {
        var store = new Mock<IUserStore<TUser>>();
        var options = Mock.Of<IOptions<IdentityOptions>>(o => o.Value == new IdentityOptions());
        var pwdHasher = new PasswordHasher<TUser>();
        var userValidators = Array.Empty<IUserValidator<TUser>>();
        var pwdValidators = Array.Empty<IPasswordValidator<TUser>>();
        var normalizer = new UpperInvariantLookupNormalizer();
        var errors = new IdentityErrorDescriber();
        var logger = new Mock<ILogger<UserManager<TUser>>>().Object;

        return new Mock<UserManager<TUser>>(store.Object, options, pwdHasher,
            userValidators, pwdValidators, normalizer, errors, null!, logger);
    }

    public static Mock<SignInManager<TUser>> CreateSignInManagerMock<TUser>(UserManager<TUser> um) where TUser : class
    {
        var contextAccessor = new Mock<IHttpContextAccessor>();
        var claimsFactory = new Mock<IUserClaimsPrincipalFactory<TUser>>();
        var options = Mock.Of<IOptions<IdentityOptions>>(o => o.Value == new IdentityOptions());
        var logger = new Mock<ILogger<SignInManager<TUser>>>().Object;

        return new Mock<SignInManager<TUser>>(um, contextAccessor.Object, claimsFactory.Object,
            options, logger, null!, null!);
    }
}
