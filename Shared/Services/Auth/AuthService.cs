using Databases.Core;
using Databases.Core.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
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
    ILogger<AuthService> log) : IAuthService
{
    public async Task<(ApplicationUser User, string Token)> Register(PostAuthRegisterRequest request, CancellationToken ct)
    {
        var existingUser = await userManager.FindByEmailAsync(request.Email);
        if (existingUser is not null)
            throw new InvalidOperationException("Email is already registered.");

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        var result = await userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            log.LogError("Registration failed for {Email}: {Errors}", request.Email, errors);
            throw new InvalidOperationException($"Registration failed: {errors}");
        }

        var token = "generate-jwt-token-here"; // Replace with actual JWT generation

        return (user, token);
    }

    public Task<(ApplicationUser User, string Token)> Login(PostAuthLoginRequest request, CancellationToken ct)
    {
        throw new NotImplementedException("Implement JWT login logic here.");
    }
}
