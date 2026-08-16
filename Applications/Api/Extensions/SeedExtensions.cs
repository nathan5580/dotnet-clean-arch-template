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
            await SeedRoles(roleManager).ConfigureAwait(false);

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

    private static async Task SeedRoles(RoleManager<ApplicationRole> roleManager)
    {
        string[] roles = [AppRoles.SuperAdmin, AppRoles.User];

        foreach (var role in roles)
        {
            if (await roleManager.RoleExistsAsync(role).ConfigureAwait(false))
                continue;

            await roleManager.CreateAsync(new ApplicationRole { Name = role }).ConfigureAwait(false);
        }
    }
}
