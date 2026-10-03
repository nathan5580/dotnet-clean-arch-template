using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Api.Authorization;
using Shared.Resources.Auth;
using Shared.Resources.Enums;
using Shared.Resources.HTTP.Auth.POST;
using Shared.Resources.HTTP.Catalog.GET;
using Shared.Resources.HTTP.Catalog.POST;
using Shared.Resources.HTTP.Common;

namespace Api.Tests;

public sealed class ProductsControllerTests : IClassFixture<WebAppFactory>
{
    private readonly WebAppFactory _factory;
    private readonly HttpClient _client;

    public ProductsControllerTests(WebAppFactory factory)
    {
        _factory = factory;

        _client = factory.CreateClient();
    }

    private async Task<(string UserId, string Token)> RegisterAndGetToken()
    {
        await _factory.SeedAuthorization();

        var request = new PostAuthRegisterRequest
        {
            Email = $"products-{Guid.NewGuid():N}@example.com",
            Password = "Password123!",
            ConfirmPassword = "Password123!"
        };

        var response = await _client.PostAsJsonAsync("/api/auth/register", request);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<PostAuthResponse>>();
        return (body!.Data!.User.UserId, body.Data.Token!);
    }

    [Fact]
    public async Task GetProducts_WithoutToken_Returns401()
    {
        var response = await _client.GetAsync("/api/products");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PostProduct_WithoutWriteRight_ReturnsForbiddenUntilRoleGranted()
    {
        var (userId, token) = await RegisterAndGetToken();

        var request = new PostProductRequest
        {
            Name = "Integration Widget",
            Description = "Created in an integration test",
            Price = 42.00m,
            Category = ProductCategory.Electronics
        };

        using var deniedMessage = new HttpRequestMessage(HttpMethod.Post, "/api/products")
        {
            Content = JsonContent.Create(request, options: WebAppFactory.JsonOptions)
        };
        deniedMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var deniedResponse = await _client.SendAsync(deniedMessage);
        Assert.Equal(HttpStatusCode.Forbidden, deniedResponse.StatusCode);

        await _factory.AddRoleToUser(userId, AppRoles.SuperAdmin);

        using var grantedMessage = new HttpRequestMessage(HttpMethod.Post, "/api/products")
        {
            Content = JsonContent.Create(request, options: WebAppFactory.JsonOptions)
        };
        grantedMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var grantedResponse = await _client.SendAsync(grantedMessage);
        Assert.Equal(HttpStatusCode.Created, grantedResponse.StatusCode);

        var body = await grantedResponse.Content.ReadFromJsonAsync<ApiResponse<GetProduct>>(WebAppFactory.JsonOptions);
        Assert.NotNull(body);
        Assert.Equal("Integration Widget", body!.Data!.Name);
    }

    [Fact]
    public async Task GetProducts_WithValidToken_Returns200()
    {
        var (userId, token) = await RegisterAndGetToken();
        await _factory.AddRoleToUser(userId, AppRoles.SuperAdmin);

        var createRequest = new PostProductRequest
        {
            Name = $"Listed-{Guid.NewGuid():N}",
            Price = 5.00m,
            Category = ProductCategory.General
        };

        using var createMessage = new HttpRequestMessage(HttpMethod.Post, "/api/products")
        {
            Content = JsonContent.Create(createRequest, options: WebAppFactory.JsonOptions)
        };
        createMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var createResponse = await _client.SendAsync(createMessage);
        createResponse.EnsureSuccessStatusCode();

        using var listMessage = new HttpRequestMessage(HttpMethod.Get, "/api/products");
        listMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var listResponse = await _client.SendAsync(listMessage);
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);

        var body = await listResponse.Content.ReadFromJsonAsync<ApiResponse<List<GetProduct>>>(WebAppFactory.JsonOptions);
        Assert.NotNull(body);
        Assert.True(body!.Success);
        Assert.Contains(body.Data!, product => product.Name == createRequest.Name);
    }

    [Fact]
    public async Task GetProducts_WhenRightIsRevokedAndGranted_ChangesAccessImmediately()
    {
        var (_, token) = await RegisterAndGetToken();

        await _factory.SetRightForRole(AppRoles.User, AppRights.ProductsRead, false);
        await _factory.SeedAuthorization();
        using var deniedMessage = new HttpRequestMessage(HttpMethod.Get, "/api/products");
        deniedMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var deniedResponse = await _client.SendAsync(deniedMessage);
        Assert.Equal(HttpStatusCode.Forbidden, deniedResponse.StatusCode);

        await _factory.SetRightForRole(AppRoles.User, AppRights.ProductsRead, true);
        using var grantedMessage = new HttpRequestMessage(HttpMethod.Get, "/api/products");
        grantedMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var grantedResponse = await _client.SendAsync(grantedMessage);
        Assert.Equal(HttpStatusCode.OK, grantedResponse.StatusCode);
    }
}
