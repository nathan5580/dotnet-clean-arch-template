using Databases.Core.Entities;
using Shared.Mapping.Auth;

namespace Shared.Tests;

public sealed class AuthMapperTests
{
    [Fact]
    public void ToGetMe_WithValidUser_MapsCorrectly()
    {
        var mapper = new AuthMapper();

        var user = new ApplicationUser
        {
            UserName = "test@example.com",
            Email = "test@example.com",
            CreatedAt = new DateTime(2024, 1, 1),
            IsActive = true
        };

        var result = mapper.ToGetMe(user);

        Assert.Equal(user.Id.ToString(), result.UserId);
        Assert.Equal(user.Email, result.Email);
        Assert.Equal(user.CreatedAt, result.CreatedAt);
        Assert.True(result.IsActive);
    }
}
