namespace Api;

public static class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddAppServices(builder.Configuration, builder.Environment);

        var app = builder.Build();

        app.UseAppMiddleware(app.Environment);

        await app.SeedDatabase();

        await app.RunAsync();
    }
}
