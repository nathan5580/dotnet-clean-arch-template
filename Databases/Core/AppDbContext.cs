using System.Reflection;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace Databases.Core;

public sealed class AppDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, string>
{
    /// <summary>
    /// Additional assemblies to scan for IEntityTypeConfiguration implementations.
    /// Register external configuration assemblies here before AddDbContext is called.
    /// </summary>
    public static readonly List<Assembly> ExtraConfigurationAssemblies = [];

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Product> Products => Set<Product>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.HasDefaultSchema("Auth");

        // Load per-context configurations
        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        foreach (var assembly in ExtraConfigurationAssemblies)
            builder.ApplyConfigurationsFromAssembly(assembly);
    }
}
