using Databases.Core;
using Microsoft.EntityFrameworkCore;

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

            // Seed roles, admin user, demo data here
            // Idempotent — skip if already exists
        }
        catch (Exception ex)
        {
            var loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger(nameof(SeedExtensions));
            logger.LogWarning(ex, "Database seeding skipped — DB may not be ready yet.");
        }
    }
}
