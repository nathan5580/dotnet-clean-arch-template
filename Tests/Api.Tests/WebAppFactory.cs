using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Databases.Core;
using Databases.Core.Entities;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Shared.Resources.Auth;

namespace Api.Tests;

public sealed class WebAppFactory : WebApplicationFactory<Api.Program>
{
    // Shared JSON options that mirror the API's JsonStringEnumConverter so client-side
    // serialization and deserialization round-trip enums as their string names.
    public static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    // Each factory instance (one per test class) gets its own isolated InMemory store so
    // role seeding and registered users from one test class can't leak into another and
    // trip Identity's single-row lookups ("Sequence contains more than one element").
    private readonly string _databaseName = $"TestDb_{Guid.NewGuid():N}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {

        builder.ConfigureAppConfiguration((_, config) =>
        {

            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "test-secret-key-that-is-at-least-32-bytes-long!!",
                ["Jwt:Issuer"] = "test-issuer",
                ["Jwt:Audience"] = "test-audience",
                ["Jwt:AccessTokenExpiryMinutes"] = "60"
            });

        });

        builder.ConfigureServices(services =>
        {

            // Remove EF relational descriptors so InMemory provider can be used
            var efDescriptors = services
                .Where(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>)
                         || d.ServiceType == typeof(DbContextOptions)
                         || (d.ServiceType.IsGenericType
                             && d.ServiceType.GetGenericTypeDefinition().Name.StartsWith("IDbContextOptionsConfiguration")))
                .ToList();

            foreach (var descriptor in efDescriptors)
                services.Remove(descriptor);

            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase(_databaseName));

            // Remove all IConfigureOptions<JwtBearerOptions> registered by AddJwtBearer
            // (they captured an empty key from appsettings.json) and replace with test key
            var jwtConfigDescriptors = services
                .Where(d => d.ServiceType == typeof(IConfigureOptions<JwtBearerOptions>)
                         || d.ServiceType == typeof(IPostConfigureOptions<JwtBearerOptions>)
                         || d.ServiceType == typeof(IOptionsChangeTokenSource<JwtBearerOptions>))
                .ToList();

            foreach (var descriptor in jwtConfigDescriptors)
                services.Remove(descriptor);

            var testKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("test-secret-key-that-is-at-least-32-bytes-long!!"));

            services.Configure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
            {

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = "test-issuer",
                    ValidAudience = "test-audience",
                    IssuerSigningKey = testKey,
                    ClockSkew = TimeSpan.Zero
                };

            });

        });

    }

    // The InMemory provider can't run migrations, so the startup seeder is skipped in tests.
    // Seed the application roles directly so registration (AddToRoleAsync) succeeds.
    public async Task SeedRoles()
    {

        using var scope = Services.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();

        foreach (var role in new[] { AppRoles.SuperAdmin, AppRoles.User })
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new ApplicationRole { Name = role });
        }

    }
}
