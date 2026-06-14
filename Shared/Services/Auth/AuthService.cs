using Databases.Core;
using Databases.Core.Entities;
using Microsoft.AspNetCore.Identity;
using Shared.Resources.HTTP.Auth.POST;

namespace Shared.Services.Auth;

public interface IAuthService
{
    Task<(ApplicationUser User, string Token)> Register(PostAuthRegisterRequest request, CancellationToken ct);
    Task<(ApplicationUser User, string Token)> Login(PostAuthLoginRequest request, CancellationToken ct);
}

public sealed class AuthService(
    AppDbContext db,
    UserManager<ApplicationUser> userManager,
    IJwtService jwtService,
    ILogger<AuthService> log) : IAuthService
{
    public async Task<(ApplicationUser User, string Token)> Register(PostAuthRegisterRequest request, CancellationToken ct)
    {

        var existingUser = await userManager.FindByEmailAsync(request.Email).ConfigureAwait(false);
        if (existingUser is not null)
            throw new InvalidOperationException("Email is already registered.");

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        var result = await userManager.CreateAsync(user, request.Password).ConfigureAwait(false);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            log.LogError("Registration failed for {Email}: {Errors}", request.Email, errors);
            throw new InvalidOperationException($"Registration failed: {errors}");
        }

        await userManager.AddToRoleAsync(user, AppRoles.User).ConfigureAwait(false);

        var roles = await userManager.GetRolesAsync(user).ConfigureAwait(false);
        var token = jwtService.GenerateToken(user, roles);

        return (user, token);

    }

    public async Task<(ApplicationUser User, string Token)> Login(PostAuthLoginRequest request, CancellationToken ct)
    {

        var user = await userManager.FindByEmailAsync(request.Email).ConfigureAwait(false);
        if (user is null)
            throw new UnauthorizedAccessException("Invalid credentials.");

        if (!user.IsActive)
            throw new UnauthorizedAccessException("Account is inactive.");

        var passwordValid = await userManager.CheckPasswordAsync(user, request.Password).ConfigureAwait(false);
        if (!passwordValid)
            throw new UnauthorizedAccessException("Invalid credentials.");

        user.LastLoginAt = DateTime.UtcNow;
        await userManager.UpdateAsync(user).ConfigureAwait(false);

        var roles = await userManager.GetRolesAsync(user).ConfigureAwait(false);
        var token = jwtService.GenerateToken(user, roles);

        return (user, token);

    }
}
