using System.Net.Http.Headers;
using System.Net.Http.Json;
using Shared.Resources.HTTP.Auth.GET;
using Shared.Resources.HTTP.Auth.POST;
using Shared.Resources.HTTP.Common;

namespace Api.Tests;

public sealed class AuthControllerTests : IClassFixture<WebAppFactory>
{
    private readonly WebAppFactory _factory;
    private readonly HttpClient _client;

    public AuthControllerTests(WebAppFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetHealth_WhenCalled_Returns200()
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

    [Fact]
    public async Task PostRegister_WithValidRequest_ReturnsToken()
    {
        await _factory.SeedRoles();

        var request = new PostAuthRegisterRequest
        {
            Email = $"register-{Guid.NewGuid():N}@example.com",
            Password = "Password123!",
            ConfirmPassword = "Password123!"
        };

        var response = await _client.PostAsJsonAsync("/api/auth/register", request);
        Assert.Equal(System.Net.HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<GetMe>>();
        Assert.NotNull(body);
        Assert.True(body!.Success);
        Assert.False(string.IsNullOrWhiteSpace(body.Token));
        Assert.Equal(request.Email, body.Data!.Email);
    }

    [Fact]
    public async Task GetAuthMe_WithValidToken_Returns200()
    {
        await _factory.SeedRoles();

        var request = new PostAuthRegisterRequest
        {
            Email = $"me-{Guid.NewGuid():N}@example.com",
            Password = "Password123!",
            ConfirmPassword = "Password123!"
        };

        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", request);
        registerResponse.EnsureSuccessStatusCode();

        var registerBody = await registerResponse.Content.ReadFromJsonAsync<ApiResponse<GetMe>>();
        var token = registerBody!.Token!;

        using var meRequest = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
        meRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var meResponse = await _client.SendAsync(meRequest);
        Assert.Equal(System.Net.HttpStatusCode.OK, meResponse.StatusCode);

        var meBody = await meResponse.Content.ReadFromJsonAsync<ApiResponse<GetMe>>();
        Assert.NotNull(meBody);
        Assert.Equal(request.Email, meBody!.Data!.Email);
    }
}
