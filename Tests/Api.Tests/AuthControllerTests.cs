using Shared.Resources.HTTP.Common;

namespace Api.Tests;

public sealed class AuthControllerTests : IClassFixture<WebAppFactory>
{
    private readonly HttpClient _client;

    public AuthControllerTests(WebAppFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetHealth_Returns200()
    {
        var response = await _client.GetAsync("/api/health");
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Healthy", content);
    }

    [Fact]
    public async Task GetAuthMe_WithoutToken_Returns401()
    {
        var response = await _client.GetAsync("/api/auth/me");
        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PostRegister_WithInvalidRequest_Returns400()
    {
        var content = new StringContent(
            "{}",
            System.Text.Encoding.UTF8,
            "application/json");

        var response = await _client.PostAsync("/api/auth/register", content);
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
    }
}
