using Databases.Core.Entities;
using Microsoft.AspNetCore.Identity;
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
    public async Task Register_WithExistingEmail_ThrowsInvalidOperationException()
    {
        var userManager = CreateUserManagerMock();
        var jwt = new Mock<IJwtService>();

        userManager.Setup(m => m.FindByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync(new ApplicationUser { Email = "duplicate@example.com" });

        var service = new AuthService(userManager.Object, jwt.Object, new AuthMapper(), NullLogger<AuthService>.Instance);
        var request = new PostAuthRegisterRequest
        {
            Email = "duplicate@example.com",
            Password = "Password123!",
            ConfirmPassword = "Password123!"
        };

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.Register(request, CancellationToken.None));
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
