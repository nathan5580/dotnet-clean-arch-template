using AutoMapper;
using Databases.Core.Entities;
using Shared.Mapping.Auth;
using Shared.Resources.HTTP.Auth.GET;

namespace Shared.Tests;

public sealed class AuthMapperTests
{
    [Fact]
    public void ToGetMe_WithValidUser_MapsCorrectly()
    {
        var configuration = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<AuthMappingProfile>();
        });
        configuration.AssertConfigurationIsValid();

        var mapper = configuration.CreateMapper();
        var authMapper = new AuthMapper(mapper);

        var user = new ApplicationUser
        {
            UserName = "test@example.com",
            Email = "test@example.com",
            CreatedAt = new DateTime(2024, 1, 1),
            IsActive = true
        };

        var result = authMapper.ToGetMe(user);

        Assert.Equal(user.Email, result.Email);
        Assert.Equal(user.CreatedAt, result.CreatedAt);
        Assert.True(result.IsActive);
    }
}
