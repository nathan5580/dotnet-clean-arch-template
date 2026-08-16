using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Scalar.AspNetCore;
using Serilog;
using Shared.Mapping.Auth;
using Shared.Mapping.Catalog;
using Shared.Services.Auth;
using Shared.Services.Catalog;

namespace Api.Extensions;

public static class ServiceExtensions
{
    public static void AddAppServices(this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment environment)
    {
        AddDatabase(services, configuration);

        AddIdentity(services);
        services.AddAppAuthentication(configuration);
        AddApplicationServices(services);
        AddValidation(services);
        AddQuartz(services, configuration);
        AddApiDocumentation(services);
        AddLogging(services, configuration);
        AddControllers(services);
    }

    private static void AddDatabase(IServiceCollection services, IConfiguration configuration)
    {
        // Register external EF configuration assemblies before AddDbContext
        AppDbContext.ExtraConfigurationAssemblies.Add(typeof(Databases.Auth.UserConfiguration).Assembly);
        AppDbContext.ExtraConfigurationAssemblies.Add(typeof(Databases.Catalog.ProductConfiguration).Assembly);

        // Migrations live in the Api project (Data/Migrations), not in Databases.Core
        // where AppDbContext is defined, so point EF at the Api assembly.
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                sql => sql.MigrationsAssembly(typeof(ServiceExtensions).Assembly.GetName().Name)));
    }

    private static void AddIdentity(IServiceCollection services)
    {
        services.AddIdentity<ApplicationUser, ApplicationRole>()
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();
    }

    private static void AddApplicationServices(IServiceCollection services)
    {
        services.AddSingleton<IJwtService, JwtService>();

        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IAuthMapper, AuthMapper>();
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<IProductMapper, ProductMapper>();
    }

    private static void AddValidation(IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<Shared.Resources.Validators.Auth.PostAuthLoginRequestValidator>();
    }

    private static void AddQuartz(IServiceCollection services, IConfiguration configuration)
    {
        services.AddAppQuartzJobs(configuration);
    }

    private static void AddApiDocumentation(IServiceCollection services)
    {
        services.AddOpenApi(options =>
        {
            options.AddDocumentTransformer<OpenApiDocumentTransformer>();
        });
    }

    private static void AddLogging(IServiceCollection services, IConfiguration configuration)
    {
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .Enrich.FromLogContext()
            .Enrich.WithThreadId()
            .Enrich.WithEnvironmentName()
            .WriteTo.Console()
            .WriteTo.File("logs/api-.log", rollingInterval: RollingInterval.Day)
            .CreateLogger();

        services.AddSerilog();
    }

    private static void AddControllers(IServiceCollection services)
    {
        services.AddControllers()
            .AddJsonOptions(o =>
                o.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));
    }

    public static void UseAppMiddleware(this WebApplication app, IWebHostEnvironment environment)
    {
        if (environment.IsDevelopment())
        {
            app.UseWebAssemblyDebugging();
        }

        // OpenAPI / Scalar
        app.MapOpenApi();
        app.MapScalarApiReference(options =>
        {
            options.WithTitle("{{ProjectName}} API v1");

            options.WithTheme(ScalarTheme.BluePlanet);
        });

        // Middleware pipeline
        app.UseExceptionMiddleware();
        app.UseSerilogRequestLogging();

        // Blazor WASM static files
        app.UseBlazorFrameworkFiles();
        app.UseStaticFiles();

        app.UseAuthentication();
        app.UseAuthorization();

        // Endpoints
        app.MapControllers();

        // Health check
        app.MapGet("/api/health", () => Results.Ok(new { Status = "Healthy", Timestamp = DateTimeOffset.UtcNow }));

        // SPA fallback
        app.MapFallbackToFile("index.html");
    }
}
