using System.Net.Http.Json;
using Shared.Resources.HTTP.Common;

namespace Web.Services;

public interface IApiClient
{
    Task<ApiResponse<T>> GetAsync<T>(string url, CancellationToken ct = default);
    Task<ApiResponse<T>> PostAsync<T>(string url, object body, CancellationToken ct = default);
    Task<ApiResponse<T>> PutAsync<T>(string url, object body, CancellationToken ct = default);
    Task<ApiResponse> DeleteAsync(string url, CancellationToken ct = default);
}

public sealed class ApiClient(HttpClient http) : IApiClient
{
    public async Task<ApiResponse<T>> GetAsync<T>(string url, CancellationToken ct = default)
    {
        var response = await http.GetAsync(url, ct);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<T>>(cancellationToken: ct);
        return result ?? ApiResponse<T>.Fail("Empty response");
    }

    public async Task<ApiResponse<T>> PostAsync<T>(string url, object body, CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync(url, body, ct);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<T>>(cancellationToken: ct);
        return result ?? ApiResponse<T>.Fail("Empty response");
    }

    public async Task<ApiResponse<T>> PutAsync<T>(string url, object body, CancellationToken ct = default)
    {
        var response = await http.PutAsJsonAsync(url, body, ct);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<T>>(cancellationToken: ct);
        return result ?? ApiResponse<T>.Fail("Empty response");
    }

    public async Task<ApiResponse> DeleteAsync(string url, CancellationToken ct = default)
    {
        var response = await http.DeleteAsync(url, ct);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<ApiResponse>(cancellationToken: ct);
        return result ?? ApiResponse.Fail("Empty response");
    }
}
