using Microsoft.AspNetCore.Identity;
using Shared.Mapping.Auth;
using Shared.Resources.Auth;
using Shared.Resources.HTTP.Auth.GET;
using Shared.Resources.HTTP.Auth.POST;

namespace Shared.Services.Auth;

public interface IAuthService
{
    Task<(ApplicationUser User, string Token)> Register(PostAuthRegisterRequest request, CancellationToken ct);
    Task<(ApplicationUser User, string Token)> Login(PostAuthLoginRequest request, CancellationToken ct);
    Task<GetMe> GetMe(string userId, CancellationToken ct);
}

public sealed class AuthService(
    UserManager<ApplicationUser> userManager,
    IJwtService jwtService,
    IAuthMapper authMapper,
    ILogger<AuthService> log) : IAuthService
{
    public async Task<(ApplicationUser User, string Token)> Register(PostAuthRegisterRequest request, CancellationToken ct)
    {
        var existingUser = await userManager.FindByEmailAsync(request.Email).ConfigureAwait(false);
        if (existingUser is not null)
            throw new InvalidOperationException("Registration failed.");

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
            throw new InvalidOperationException("Registration failed.");
        }

        var roleResult = await userManager.AddToRoleAsync(user, AppRoles.User).ConfigureAwait(false);
        if (!roleResult.Succeeded)
        {
            var errors = string.Join(", ", roleResult.Errors.Select(e => e.Description));
            log.LogError("Default role assignment failed for user {UserId}: {Errors}", user.Id, errors);

            var cleanupResult = await userManager.DeleteAsync(user).ConfigureAwait(false);
            if (!cleanupResult.Succeeded)
            {
                var cleanupErrors = string.Join(", ", cleanupResult.Errors.Select(e => e.Description));
                log.LogError("Registration cleanup failed for user {UserId}: {Errors}", user.Id, cleanupErrors);
            }

            throw new InvalidOperationException("Registration failed.");
        }

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
            throw new UnauthorizedAccessException("Invalid credentials.");

        var passwordValid = await userManager.CheckPasswordAsync(user, request.Password).ConfigureAwait(false);
        if (!passwordValid)
            throw new UnauthorizedAccessException("Invalid credentials.");

        user.LastLoginAt = DateTime.UtcNow;
        var updateResult = await userManager.UpdateAsync(user).ConfigureAwait(false);
        if (!updateResult.Succeeded)
        {
            var errors = string.Join(", ", updateResult.Errors.Select(e => e.Description));
            log.LogWarning("Failed to update last login for user {UserId}: {Errors}", user.Id, errors);
        }

        var roles = await userManager.GetRolesAsync(user).ConfigureAwait(false);
        var token = jwtService.GenerateToken(user, roles);

        return (user, token);
    }

    public async Task<GetMe> GetMe(string userId, CancellationToken ct)
    {
        var user = await userManager.FindByIdAsync(userId).ConfigureAwait(false);
        if (user is null)
            throw new UnauthorizedAccessException("User not found.");

        var roles = await userManager.GetRolesAsync(user).ConfigureAwait(false);

        return authMapper.ToGetMe(user) with { Roles = roles.ToList() };
    }
}
