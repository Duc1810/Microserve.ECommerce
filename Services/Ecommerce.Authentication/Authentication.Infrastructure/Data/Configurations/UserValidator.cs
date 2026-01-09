using Authentication.Domain.Entities;
using Duende.IdentityServer.Models;
using Duende.IdentityServer.Validation;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Authentication.Infrastructure.Data.Configurations;

public sealed class UserValidator : IResourceOwnerPasswordValidator
{
    private readonly UserManager<User> _userManager;
    private readonly SignInManager<User> _signInManager;

    public UserValidator(UserManager<User> userManager, SignInManager<User> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
    }

    public async Task ValidateAsync(ResourceOwnerPasswordValidationContext context)
    {
        var input = context.UserName?.Trim() ?? string.Empty;
        var password = context.Password ?? string.Empty;
        var user = await _userManager.FindByNameAsync(input);


        if (user is null && !string.IsNullOrWhiteSpace(input))
        {
            var normalizedEmail = _userManager.NormalizeEmail(input);
            user = await _userManager.Users.FirstOrDefaultAsync(
                u => u.NormalizedEmail == normalizedEmail);
        }

        if (user is null)
        {
            context.Result = new GrantValidationResult(TokenRequestErrors.InvalidGrant, "invalid_username_or_password");
            return;
        }

        var passwordOk = await _userManager.CheckPasswordAsync(user, password);
        if (!passwordOk)
        {
            context.Result = new GrantValidationResult(TokenRequestErrors.InvalidGrant, "invalid_username_or_password");
            return;
        }

        context.Result = new GrantValidationResult(
            subject: user.Id,
            authenticationMethod: "password");
    }
}
