using Microsoft.AspNetCore.Components.Web;

namespace Web;

public static class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebAssemblyHostBuilder.CreateDefault(args);
        builder.RootComponents.Add<App>("#app");
        builder.RootComponents.Add<HeadOutlet>("head::after");

        // Services
        builder.Services.AddScoped(_ => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
        builder.Services.AddScoped<IApiClient, ApiClient>();
        builder.Services.AddScoped<IToastService, ToastService>();
        builder.Services.AddScoped<LocalizationService>();
        builder.Services.AddAuthorizationCore();

        await builder.Build().RunAsync();
    }
}
