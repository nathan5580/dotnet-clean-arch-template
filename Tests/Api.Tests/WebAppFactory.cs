using Microsoft.AspNetCore.Mvc.Testing;

namespace Api.Tests;

public sealed class WebAppFactory : WebApplicationFactory<Api.Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Replace real DB with in-memory for tests
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<Databases.Core.AppDbContext>));
            if (descriptor is not null) services.Remove(descriptor);

            services.AddDbContext<Databases.Core.AppDbContext>(options =>
                options.UseInMemoryDatabase("TestDb"));
        });
    }
}
