using Microsoft.AspNetCore.Identity;

namespace Api.Extensions;

public static class SeedExtensions
{
    public static async Task SeedDatabase(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        try
        {
            var canConnect = await db.Database.CanConnectAsync().ConfigureAwait(false);
            if (!canConnect) return;

            await db.Database.MigrateAsync().ConfigureAwait(false);

            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
            await SeedAuthorization(roleManager, db).ConfigureAwait(false);

            // Seed admin user, demo data here
            // Idempotent — skip if already exists
        }
        catch (Exception ex)
        {
            var loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger(nameof(SeedExtensions));
            logger.LogWarning(ex, "Database seeding skipped — DB may not be ready yet.");

            if (app.Environment.IsProduction())
                throw;
        }
    }

    public static async Task SeedAuthorization(RoleManager<ApplicationRole> roleManager, AppDbContext db)
    {
        await SeedRoles(roleManager).ConfigureAwait(false);

        var rights = await db.Rights.ToDictionaryAsync(right => right.Code).ConfigureAwait(false);
        var initialSeed = rights.Count == 0;
        var rightDefinitions = new[]
        {
            (AppRights.ProductsRead, "Read catalog products."),
            (AppRights.ProductsWrite, "Create, update, and delete catalog products.")
        };

        foreach (var (code, description) in rightDefinitions)
        {
            if (rights.ContainsKey(code))
                continue;

            var right = new Right { RightId = Guid.NewGuid(), Code = code, Description = description };
            db.Rights.Add(right);
            rights.Add(code, right);
        }

        if (initialSeed)
        {
            var grants = new[]
            {
                (AppRoles.User, AppRights.ProductsRead),
                (AppRoles.SuperAdmin, AppRights.ProductsRead),
                (AppRoles.SuperAdmin, AppRights.ProductsWrite)
            };

            foreach (var (roleName, rightCode) in grants)
            {
                var role = await roleManager.FindByNameAsync(roleName).ConfigureAwait(false)
                    ?? throw new InvalidOperationException($"Required role '{roleName}' is missing.");
                db.RoleRights.Add(new RoleRight { RoleId = role.Id, RightId = rights[rightCode].RightId });
            }
        }

        await db.SaveChangesAsync().ConfigureAwait(false);
    }

    private static async Task SeedRoles(RoleManager<ApplicationRole> roleManager)
    {
        string[] roles = [AppRoles.SuperAdmin, AppRoles.User];

        foreach (var role in roles)
        {
            if (await roleManager.RoleExistsAsync(role).ConfigureAwait(false))
                continue;

            var result = await roleManager.CreateAsync(new ApplicationRole { Name = role }).ConfigureAwait(false);
            if (!result.Succeeded)
                throw new InvalidOperationException(
                    $"Failed to seed role '{role}': {string.Join(", ", result.Errors.Select(error => error.Description))}");
        }
    }
}
