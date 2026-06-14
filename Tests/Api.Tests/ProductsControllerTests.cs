using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Shared.Resources.HTTP.Auth.GET;
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

    private async Task<string> RegisterAndGetToken()
    {

        await _factory.SeedRoles();

        var request = new PostAuthRegisterRequest
        {
            Email = $"products-{Guid.NewGuid():N}@example.com",
            Password = "Password123!",
            ConfirmPassword = "Password123!"
        };

        var response = await _client.PostAsJsonAsync("/api/auth/register", request);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<GetMe>>();
        return body!.Token!;

    }

    [Fact]
    public async Task GetProducts_WithoutToken_Returns401()
    {

        var response = await _client.GetAsync("/api/products");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

    }

    [Fact]
    public async Task PostProduct_WithValidTokenAndRequest_Returns201()
    {

        var token = await RegisterAndGetToken();

        var request = new PostProductRequest
        {
            Name = "Integration Widget",
            Description = "Created in an integration test",
            Price = 42.00m,
            Category = "Electronics"
        };

        using var message = new HttpRequestMessage(HttpMethod.Post, "/api/products")
        {
            Content = JsonContent.Create(request)
        };
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(message);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<GetProduct>>();
        Assert.NotNull(body);
        Assert.True(body!.Success);
        Assert.Equal("Integration Widget", body.Data!.Name);
        Assert.Equal("Electronics", body.Data.Category);

    }

    [Fact]
    public async Task GetProducts_WithValidToken_Returns200()
    {

        var token = await RegisterAndGetToken();

        var createRequest = new PostProductRequest
        {
            Name = $"Listed-{Guid.NewGuid():N}",
            Price = 5.00m,
            Category = "General"
        };

        using var createMessage = new HttpRequestMessage(HttpMethod.Post, "/api/products")
        {
            Content = JsonContent.Create(createRequest)
        };
        createMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var createResponse = await _client.SendAsync(createMessage);
        createResponse.EnsureSuccessStatusCode();

        using var listMessage = new HttpRequestMessage(HttpMethod.Get, "/api/products");
        listMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var listResponse = await _client.SendAsync(listMessage);
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);

        var body = await listResponse.Content.ReadFromJsonAsync<ApiResponse<List<GetProduct>>>();
        Assert.NotNull(body);
        Assert.True(body!.Success);
        Assert.Contains(body.Data!, p => p.Name == createRequest.Name);

    }
}
