using Databases.Core.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;
using Shared.Mapping.Auth;
using Shared.Resources.Auth;
using Shared.Resources.HTTP.Auth.POST;
using Shared.Services.Auth;

namespace Shared.Tests;

public sealed class AuthServiceTests
{
    private static Mock<UserManager<ApplicationUser>> CreateUserManagerMock()
    {
        var store = new Mock<IUserStore<ApplicationUser>>();

        return new Mock<UserManager<ApplicationUser>>(
            store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
    }

    [Fact]
    public async Task Register_WithValidRequest_ReturnsToken()
    {
        var userManager = CreateUserManagerMock();
        var jwt = new Mock<IJwtService>();

        userManager.Setup(m => m.FindByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync((ApplicationUser?)null);
        userManager.Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);
        userManager.Setup(m => m.AddToRoleAsync(It.IsAny<ApplicationUser>(), AppRoles.User))
            .ReturnsAsync(IdentityResult.Success);
        userManager.Setup(m => m.GetRolesAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(new List<string> { AppRoles.User });
        jwt.Setup(j => j.GenerateToken(It.IsAny<ApplicationUser>(), It.IsAny<IList<string>>()))
            .Returns("generated-token");

        var service = new AuthService(userManager.Object, jwt.Object, new AuthMapper(), NullLogger<AuthService>.Instance);
        var request = new PostAuthRegisterRequest
        {
            Email = "new@example.com",
            Password = "Password123!",
            ConfirmPassword = "Password123!"
        };

        var (user, token) = await service.Register(request, CancellationToken.None);

        Assert.Equal("new@example.com", user.Email);
        Assert.Equal("generated-token", token);
        userManager.Verify(m => m.AddToRoleAsync(It.IsAny<ApplicationUser>(), AppRoles.User), Times.Once);
        jwt.Verify(j => j.GenerateToken(It.IsAny<ApplicationUser>(), It.IsAny<IList<string>>()), Times.Once);
    }

    [Fact]
    public async Task Register_WhenRoleAssignmentFails_DeletesUserAndReturnsGenericFailure()
    {
        var userManager = CreateUserManagerMock();
        var jwt = new Mock<IJwtService>();

        userManager.Setup(m => m.FindByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync((ApplicationUser?)null);
        userManager.Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);
        userManager.Setup(m => m.AddToRoleAsync(It.IsAny<ApplicationUser>(), AppRoles.User))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "role assignment details" }));
        userManager.Setup(m => m.DeleteAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(IdentityResult.Success);

        var service = new AuthService(userManager.Object, jwt.Object, new AuthMapper(), NullLogger<AuthService>.Instance);
        var request = new PostAuthRegisterRequest
        {
            Email = "new@example.com",
            Password = "Password123!",
            ConfirmPassword = "Password123!"
        };

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.Register(request, CancellationToken.None));

        Assert.Equal("Registration failed.", exception.Message);
        userManager.Verify(m => m.DeleteAsync(It.IsAny<ApplicationUser>()), Times.Once);
        jwt.Verify(j => j.GenerateToken(It.IsAny<ApplicationUser>(), It.IsAny<IList<string>>()), Times.Never);
    }

    [Fact]
    public async Task Register_WhenIdentityRejectsUser_ReturnsGenericFailure()
    {
        var userManager = CreateUserManagerMock();
        var jwt = new Mock<IJwtService>();

        userManager.Setup(m => m.FindByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync((ApplicationUser?)null);
        userManager.Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "password policy details" }));

        var service = new AuthService(userManager.Object, jwt.Object, new AuthMapper(), NullLogger<AuthService>.Instance);
        var request = new PostAuthRegisterRequest
        {
            Email = "new@example.com",
            Password = "Password123!",
            ConfirmPassword = "Password123!"
        };

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.Register(request, CancellationToken.None));

        Assert.Equal("Registration failed.", exception.Message);
        jwt.Verify(j => j.GenerateToken(It.IsAny<ApplicationUser>(), It.IsAny<IList<string>>()), Times.Never);
    }

    [Fact]
    public async Task Login_WithInactiveAccount_UsesGenericUnauthorizedMessage()
    {
        var userManager = CreateUserManagerMock();
        var jwt = new Mock<IJwtService>();

        userManager.Setup(m => m.FindByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync(new ApplicationUser { Email = "user@example.com", IsActive = false });

        var service = new AuthService(userManager.Object, jwt.Object, new AuthMapper(), NullLogger<AuthService>.Instance);
        var request = new PostAuthLoginRequest
        {
            Email = "user@example.com",
            Password = "Password123!"
        };

        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => service.Login(request, CancellationToken.None));

        Assert.Equal("Invalid credentials.", exception.Message);
    }

    [Fact]
    public async Task Login_WhenLastLoginUpdateFails_LogsWarningAndReturnsToken()
    {
        var userManager = CreateUserManagerMock();
        var jwt = new Mock<IJwtService>();
        var logger = new Mock<ILogger<AuthService>>();
        var user = new ApplicationUser { Id = "user-id", Email = "user@example.com", IsActive = true };

        userManager.Setup(m => m.FindByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync(user);
        userManager.Setup(m => m.CheckPasswordAsync(user, It.IsAny<string>()))
            .ReturnsAsync(true);
        userManager.Setup(m => m.UpdateAsync(user))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "update details" }));
        userManager.Setup(m => m.GetRolesAsync(user))
            .ReturnsAsync([]);
        jwt.Setup(j => j.GenerateToken(user, It.IsAny<IList<string>>()))
            .Returns("generated-token");

        var service = new AuthService(userManager.Object, jwt.Object, new AuthMapper(), logger.Object);
        var request = new PostAuthLoginRequest
        {
            Email = "user@example.com",
            Password = "Password123!"
        };

        var (_, token) = await service.Login(request, CancellationToken.None);

        Assert.Equal("generated-token", token);
        logger.Verify(log => log.Log(
            LogLevel.Warning,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((state, _) => state.ToString()!.Contains("Failed to update last login")),
            It.IsAny<Exception?>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [Fact]
    public async Task Login_WithBadPassword_ThrowsUnauthorizedAccessException()
    {
        var userManager = CreateUserManagerMock();
        var jwt = new Mock<IJwtService>();

        userManager.Setup(m => m.FindByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync(new ApplicationUser { Email = "user@example.com", IsActive = true });
        userManager.Setup(m => m.CheckPasswordAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .ReturnsAsync(false);

        var service = new AuthService(userManager.Object, jwt.Object, new AuthMapper(), NullLogger<AuthService>.Instance);
        var request = new PostAuthLoginRequest
        {
            Email = "user@example.com",
            Password = "WrongPassword!"
        };

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => service.Login(request, CancellationToken.None));
    }
}
