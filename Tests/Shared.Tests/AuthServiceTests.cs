using Databases.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Shared.Resources.HTTP.Auth.POST;
using Shared.Services.Auth;

namespace Shared.Tests;

public sealed class AuthServiceTests
{
    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task Register_WithValidRequest_ReturnsUser()
    {
        // TODO: Implement with real UserManager setup or mock
        // Arrange
        var db = CreateDbContext();
        var request = new PostAuthRegisterRequest
        {
            Email = "test@example.com",
            Password = "Password123!",
            ConfirmPassword = "Password123!"
        };

        // Act & Assert
        // var service = new AuthService(db, userManager, NullLogger<AuthService>.Instance);
        // var (user, token) = await service.Register(request, CancellationToken.None);
        // Assert.NotNull(user);
        // Assert.Equal("test@example.com", user.Email);
    }

    [Fact]
    public async Task Register_WithExistingEmail_ThrowsInvalidOperationException()
    {
        // Arrange
        var db = CreateDbContext();
        var request = new PostAuthRegisterRequest
        {
            Email = "duplicate@example.com",
            Password = "Password123!",
            ConfirmPassword = "Password123!"
        };

        // TODO: Setup existing user and test duplicate registration

        // Act & Assert
        // var service = new AuthService(db, userManager, NullLogger<AuthService>.Instance);
        // await Assert.ThrowsAsync<InvalidOperationException>(
        //     () => service.Register(request, CancellationToken.None));
    }
}
