using Api.Authorization;
using Api.Middleware;
using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Quartz;
using Scalar.AspNetCore;
using Serilog;
using Shared.Jobs;
using Shared.Mapping.Auth;
using Shared.Mapping.Catalog;
using Shared.Services.Auth;
using Shared.Services.Catalog;

namespace Api.Extensions;

public static class ServiceExtensions
{
    public static void AddAppServices(this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment environment)
    {
        // Register external EF configuration assemblies before AddDbContext
        AppDbContext.ExtraConfigurationAssemblies.Add(typeof(Databases.Auth.UserConfiguration).Assembly);
        AppDbContext.ExtraConfigurationAssemblies.Add(typeof(Databases.Catalog.ProductConfiguration).Assembly);

        // Database — migrations live in the Api project (Data/Migrations), not in
        // Databases.Core where AppDbContext is defined, so point EF at the Api assembly.
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                sql => sql.MigrationsAssembly(typeof(ServiceExtensions).Assembly.GetName().Name)));

        // Identity + Auth
        services.AddIdentity<ApplicationUser, ApplicationRole>()
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

        services.AddAppAuthentication(configuration);
        services.AddAppAuthorization();

        // Scoped services
        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IAuthMapper, AuthMapper>();
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<IProductMapper, ProductMapper>();

        // FluentValidation
        services.AddValidatorsFromAssemblyContaining<Shared.Resources.Validators.Auth.PostAuthLoginRequestValidator>();

        // Quartz
        services.AddAppQuartzJobs(configuration);

        // API docs
        services.AddOpenApi(options =>
        {
            options.AddDocumentTransformer<OpenApiDocumentTransformer>();
        });

        // Serilog
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .Enrich.FromLogContext()
            .Enrich.WithThreadId()
            .Enrich.WithEnvironmentName()
            .WriteTo.Console()
            .WriteTo.File("logs/api-.log", rollingInterval: RollingInterval.Day)
            .CreateLogger();

        services.AddSerilog();

        // API versioning
        services.AddApiVersioning(options =>
        {
            options.DefaultApiVersion = new ApiVersion(1, 0);
            options.AssumeDefaultVersionWhenUnspecified = true;
        });

        // Controllers
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
