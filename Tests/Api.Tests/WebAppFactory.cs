using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Api.Extensions;
using Databases.Core;
using Databases.Core.Entities;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

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

    // The InMemory provider can't run migrations, so seed the authorization rows directly.
    public async Task SeedAuthorization()
    {
        using var scope = Services.CreateScope();

        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        await SeedExtensions.SeedAuthorization(roleManager, db);
    }

    public async Task AddRoleToUser(string userId, string roleName)
    {
        using var scope = Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.FindByIdAsync(userId)
            ?? throw new InvalidOperationException($"Test user '{userId}' was not found.");

        var result = await userManager.AddToRoleAsync(user, roleName);

        if (!result.Succeeded)
            throw new InvalidOperationException(string.Join(", ", result.Errors.Select(error => error.Description)));
    }

    public async Task SetRightForRole(string roleName, string rightCode, bool enabled)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        var role = await roleManager.FindByNameAsync(roleName)
            ?? throw new InvalidOperationException($"Test role '{roleName}' was not found.");
        var right = await db.Rights.SingleAsync(entry => entry.Code == rightCode);
        var assignment = await db.RoleRights.FindAsync(role.Id, right.RightId);

        if (enabled && assignment is null)
            db.RoleRights.Add(new RoleRight { RoleId = role.Id, RightId = right.RightId });
        else if (!enabled && assignment is not null)
            db.RoleRights.Remove(assignment);

        await db.SaveChangesAsync();
    }
}
