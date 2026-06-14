# Template Polish — Implementation Plan

> ⚠️ **SUPERSEDED (2026-06-14).** This plan deletes Blazor; that decision was reversed — Blazor is kept, co-hosted. Do **not** execute Task 1. The non-Blazor tasks (un-seal, ExceptionMiddleware, JwtService, Login, tests) are re-homed into the `2026-06-14` specs/plans. Retained for reference only.

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make `dotnet-clean-arch-template` a fully professional, convention-correct reference for new .NET 10 projects — pure API, no Blazor frontend.

**Architecture:** ASP.NET Core 10 Web API co-hosted with EF Core + Identity. Shared business logic in sealed services, convention tests enforce rules at every push. Auth is scaffolded with stubs so each new project implements JWT generation once.

**Tech Stack:** .NET 10, ASP.NET Core, EF Core 10, SQL Server, ASP.NET Identity, FluentValidation, AutoMapper, Quartz.NET, Serilog, Scalar, xUnit, Moq, Coverlet.

---

## File Map

**Created:**
- `Shared/Resources/Auth/AppRoles.cs` — role name constants (moved out of Api)
- `Shared/Services/Auth/JwtService.cs` — `IJwtService` + `JwtService` stub
- `Applications/Api/Authorization/AuthenticatedController.cs` — base for all authenticated feature controllers
- `Tests/Api.Tests/ConventionTests.cs` — controller-layer architecture tests

**Modified:**
- `{{ProjectName}}.slnx` — remove Web + Web.Tests entries
- `Applications/Api/Api.csproj` — remove Web reference + Blazor package
- `Directory.Packages.props` — remove 4 Blazor packages
- `Applications/Api/Extensions/ServiceExtensions.cs` — remove Blazor middleware, register IJwtService
- `Applications/Api/Extensions/SeedExtensions.cs` — seed SuperAdmin + User roles
- `Applications/Api/Authorization/AppPermissions.cs` — remove AppRoles class
- `Applications/Api/GlobalUsings.cs` — add `Shared.Resources.Auth` using
- `Applications/Api/Controllers/Auth/AuthController.cs` — remove sealed, fix GetMe roles
- `Applications/Api/Middleware/ExceptionMiddleware.cs` — return ApiResponse.Fail()
- `Databases/Core/Entities/Entities.cs` — remove sealed from ApplicationUser, ApplicationRole, UserActionAudit
- `Shared/Services/Auth/AuthService.cs` — remove unused db param, add IJwtService, implement Login
- `Shared/Services/GlobalUsings.cs` — add Shared.Resources.Auth + Microsoft.AspNetCore.Identity usings
- `Tests/Shared.Tests/ConventionTests.cs` — fix regex, add Entities/Mappers tests, remove Api references
- `Tests/Shared.Tests/AuthServiceTests.cs` — implement with Moq
- `Tests/Shared.Tests/GlobalUsings.cs` — add Moq + Identity usings
- `Tests/Api.Tests/WebAppFactory.cs` — call EnsureCreatedAsync
- `Tests/Api.Tests/AuthControllerTests.cs` — assert response body shape
- `.github/workflows/ci.yml` — remove Node + Web steps
- `AGENTS.md` — remove Blazor, add JwtService/AuthenticatedController guidance
- `CLAUDE.md` — remove Blazor sections, update stack + run commands
- `.github/copilot-instructions.md` — remove Web references

**Deleted:**
- `Applications/Web/` (entire directory)
- `Tests/Web.Tests/` (entire directory)

---

## Task 1: Remove Web project and Web.Tests

**Files:**
- Delete: `Applications/Web/` (entire directory)
- Delete: `Tests/Web.Tests/` (entire directory)
- Modify: `{{ProjectName}}.slnx`
- Modify: `Applications/Api/Api.csproj`
- Modify: `Directory.Packages.props`
- Modify: `Applications/Api/Extensions/ServiceExtensions.cs`

- [ ] **Step 1: Delete the Web and Web.Tests directories**

```bash
rm -rf Applications/Web
rm -rf Tests/Web.Tests
```

- [ ] **Step 2: Update solution file — remove Web and Web.Tests entries**

Replace the full content of `{{ProjectName}}.slnx` with:

```xml
<Solution>
  <Folder Name="/Applications/">
    <Project Path="Applications/Api/Api.csproj" />
  </Folder>
  <Folder Name="/Databases/">
    <Project Path="Databases/Auth/Auth.csproj" />
    <Project Path="Databases/Core/Core.csproj" />
  </Folder>
  <Folder Name="/Shared/">
    <Project Path="Shared/Jobs/Jobs.csproj" />
    <Project Path="Shared/Mapping/Mapping.csproj" />
    <Project Path="Shared/Resources/Resources.csproj" />
    <Project Path="Shared/Services/Services.csproj" />
  </Folder>
  <Folder Name="/Tests/">
    <Project Path="Tests/Api.Tests/Api.Tests.csproj" />
    <Project Path="Tests/Shared.Tests/Shared.Tests.csproj" />
  </Folder>
</Solution>
```

- [ ] **Step 3: Update Api.csproj — remove Web reference and Blazor package**

In `Applications/Api/Api.csproj`, remove these two lines:
```xml
<ProjectReference Include="..\Web\Web.csproj" ReferenceOutputAssembly="false" />
```
```xml
<PackageReference Include="Microsoft.AspNetCore.Components.WebAssembly.Server" />
```

- [ ] **Step 4: Remove Blazor packages from Directory.Packages.props**

Remove these four lines from `Directory.Packages.props`:
```xml
<PackageVersion Include="Microsoft.AspNetCore.Components.Authorization" Version="10.0.7" />
<PackageVersion Include="Microsoft.AspNetCore.Components.WebAssembly" Version="10.0.7" />
<PackageVersion Include="Microsoft.AspNetCore.Components.WebAssembly.DevServer" Version="10.0.7" />
<PackageVersion Include="Microsoft.AspNetCore.Components.WebAssembly.Server" Version="10.0.7" />
```

- [ ] **Step 5: Remove Blazor from the middleware pipeline**

Replace `ServiceExtensions.cs` with the cleaned version (no Blazor calls):

`Applications/Api/Extensions/ServiceExtensions.cs`:
```csharp
using Api.Authorization;
using Api.Middleware;
using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Quartz;
using Scalar.AspNetCore;
using Serilog;
using Shared.Jobs;
using Shared.Mapping.Auth;
using Shared.Services.Auth;

namespace Api.Extensions;

public static class ServiceExtensions
{
    public static void AddAppServices(this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment environment)
    {
        // Database
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

        // Identity + Auth
        services.AddIdentity<ApplicationUser, ApplicationRole>()
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

        services.AddAppAuthentication(configuration);
        services.AddAppAuthorization();

        // AutoMapper
        services.AddSingleton<IMapper>(sp =>
            new MapperConfiguration(cfg =>
            {
                cfg.AddMaps(typeof(AuthMappingProfile).Assembly);
            }).CreateMapper());

        // Scoped services
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IAuthMapper, AuthMapper>();
        services.AddScoped<IJwtService, JwtService>();

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
        services.AddControllers();
    }

    public static void UseAppMiddleware(this WebApplication app, IWebHostEnvironment environment)
    {
        if (environment.IsDevelopment())
        {
            app.MapOpenApi();
            app.MapScalarApiReference(options =>
            {
                options.WithTitle("{{ProjectName}} API v1");
                options.WithTheme(ScalarTheme.BluePlanet);
            });
        }

        app.UseExceptionMiddleware();
        app.UseSerilogRequestLogging();

        app.UseStaticFiles();

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();

        app.MapGet("/api/health", () => Results.Ok(new { Status = "Healthy", Timestamp = DateTimeOffset.UtcNow }));
    }
}
```

- [ ] **Step 6: Verify it builds**

```bash
dotnet build {{ProjectName}}.slnx --configuration Release
```

Expected: 0 errors, 0 warnings.

- [ ] **Step 7: Commit**

```bash
git add -A
git commit -m "Template - Remove Blazor Web project and Web.Tests"
```

---

## Task 2: Fix sealed violations on entities and controller

**Files:**
- Modify: `Databases/Core/Entities/Entities.cs`
- Modify: `Applications/Api/Controllers/Auth/AuthController.cs`

- [ ] **Step 1: Remove sealed from entities**

Replace `Databases/Core/Entities/Entities.cs` with:

```csharp
using Microsoft.AspNetCore.Identity;

namespace Databases.Core.Entities;

public class ApplicationUser : IdentityUser
{
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }
    public bool IsActive { get; set; } = true;

    [NotMapped]
    public new Guid Id
    {
        get => Guid.Parse(base.Id);
        set => base.Id = value.ToString();
    }
}

public class ApplicationRole : IdentityRole
{
    [NotMapped]
    public new Guid Id
    {
        get => Guid.Parse(base.Id);
        set => base.Id = value.ToString();
    }
}

public class UserActionAudit
{
    public Guid AuditId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string? TargetType { get; set; }
    public string? TargetId { get; set; }
    public string? IpAddress { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    [NotMapped]
    public Guid Id { get => AuditId; set => AuditId = value; }

    public ApplicationUser User { get; set; } = null!;
}
```

- [ ] **Step 2: Remove sealed from AuthController**

In `Applications/Api/Controllers/Auth/AuthController.cs`, change the class declaration from:

```csharp
public sealed class AuthController(...)
```

to:

```csharp
public class AuthController(...)
```

- [ ] **Step 3: Build to confirm no regressions**

```bash
dotnet build {{ProjectName}}.slnx --configuration Release
```

Expected: 0 errors, 0 warnings.

- [ ] **Step 4: Commit**

```bash
git add Databases/Core/Entities/Entities.cs Applications/Api/Controllers/Auth/AuthController.cs
git commit -m "Databases.Core - Remove sealed from entities; Api - Remove sealed from AuthController"
```

---

## Task 3: Fix ExceptionMiddleware response shape

**Files:**
- Modify: `Applications/Api/Middleware/ExceptionMiddleware.cs`

The middleware currently returns `new { Error, StatusCode }` — an inconsistent anonymous object. It must return `ApiResponse.Fail()` as JSON, matching every other response in the system.

- [ ] **Step 1: Update ExceptionMiddleware**

Replace `Applications/Api/Middleware/ExceptionMiddleware.cs` with:

```csharp
using System.Net;
using System.Text.Json;
using Shared.Resources.HTTP.Common;

namespace Api.Middleware;

public sealed class ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context).ConfigureAwait(false);
        }
        catch (KeyNotFoundException ex)
        {
            await WriteErrorResponse(context, HttpStatusCode.NotFound, ex.Message).ConfigureAwait(false);
        }
        catch (InvalidOperationException ex)
        {
            await WriteErrorResponse(context, HttpStatusCode.BadRequest, ex.Message).ConfigureAwait(false);
        }
        catch (UnauthorizedAccessException ex)
        {
            await WriteErrorResponse(context, HttpStatusCode.Unauthorized, ex.Message).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception");
            await WriteErrorResponse(context, HttpStatusCode.InternalServerError, "An unexpected error occurred.").ConfigureAwait(false);
        }
    }

    private static async Task WriteErrorResponse(HttpContext context, HttpStatusCode status, string message)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)status;
        var response = ApiResponse.Fail(message, (int)status);
        await context.Response.WriteAsync(JsonSerializer.Serialize(response, JsonOptions)).ConfigureAwait(false);
    }
}

public static class ExceptionMiddlewareExtensions
{
    public static IApplicationBuilder UseExceptionMiddleware(this IApplicationBuilder builder)
        => builder.UseMiddleware<ExceptionMiddleware>();
}
```

- [ ] **Step 2: Build**

```bash
dotnet build {{ProjectName}}.slnx --configuration Release
```

Expected: 0 errors, 0 warnings.

- [ ] **Step 3: Commit**

```bash
git add Applications/Api/Middleware/ExceptionMiddleware.cs
git commit -m "Api - ExceptionMiddleware now returns ApiResponse.Fail for consistent error shape"
```

---

## Task 4: Move AppRoles to Shared.Resources

`AuthService` (in `Shared.Services`) needs role constants. `Shared.Services` cannot reference `Api` (that would be circular). Move `AppRoles` to `Shared.Resources.Auth` which is already in the dependency chain.

**Files:**
- Create: `Shared/Resources/Auth/AppRoles.cs`
- Modify: `Applications/Api/Authorization/AppPermissions.cs`
- Modify: `Shared/Services/GlobalUsings.cs`
- Modify: `Applications/Api/GlobalUsings.cs`

- [ ] **Step 1: Create AppRoles in Shared.Resources**

Create `Shared/Resources/Auth/AppRoles.cs`:

```csharp
namespace Shared.Resources.Auth;

public static class AppRoles
{
    public const string SuperAdmin = "SuperAdmin";
    public const string User = "User";
}
```

- [ ] **Step 2: Remove AppRoles from Api.Authorization**

Replace `Applications/Api/Authorization/AppPermissions.cs` with:

```csharp
namespace Api.Authorization;

public static class AppPermissions
{
    // Auth
    public const string UsersRead = "users.read";
    public const string UsersWrite = "users.write";

    // Add your permission constants here
}
```

- [ ] **Step 3: Add Shared.Resources.Auth global using to Services**

In `Shared/Services/GlobalUsings.cs`, add:

```csharp
global using Databases.Core;
global using Databases.Core.Entities;
global using Databases.Core.Enums;
global using Microsoft.AspNetCore.Identity;
global using Microsoft.EntityFrameworkCore;
global using Microsoft.Extensions.Logging;
global using Shared.Resources.Auth;
global using Shared.Resources.HTTP.Auth;
global using Shared.Resources.HTTP.Auth.GET;
global using Shared.Resources.HTTP.Auth.POST;

// Entity type aliases
global using UserEntity = Databases.Core.Entities.ApplicationUser;
```

- [ ] **Step 4: Add Shared.Resources.Auth global using to Api**

In `Applications/Api/GlobalUsings.cs`, add the `Shared.Resources.Auth` using:

```csharp
global using Microsoft.AspNetCore.Authentication.JwtBearer;
global using Microsoft.AspNetCore.Authorization;
global using Microsoft.AspNetCore.Mvc;
global using Microsoft.EntityFrameworkCore;
global using Microsoft.IdentityModel.Tokens;
global using Api.Authorization;
global using Api.Controllers;
global using Api.Extensions;
global using Api.Middleware;
global using Api.OpenApi;
global using Databases.Core;
global using Shared.Jobs;
global using Shared.Mapping;
global using Shared.Resources;
global using Shared.Resources.Auth;
global using Shared.Resources.Enums;
global using Shared.Resources.HTTP;
global using Shared.Services;
```

- [ ] **Step 5: Build**

```bash
dotnet build {{ProjectName}}.slnx --configuration Release
```

Expected: 0 errors, 0 warnings.

- [ ] **Step 6: Commit**

```bash
git add Shared/Resources/Auth/AppRoles.cs Applications/Api/Authorization/AppPermissions.cs Shared/Services/GlobalUsings.cs Applications/Api/GlobalUsings.cs
git commit -m "Shared.Resources - Move AppRoles to Shared.Resources.Auth for cross-layer access"
```

---

## Task 5: Create JwtService stub

**Files:**
- Create: `Shared/Services/Auth/JwtService.cs`

- [ ] **Step 1: Create JwtService**

Create `Shared/Services/Auth/JwtService.cs`:

```csharp
using Microsoft.Extensions.Configuration;

namespace Shared.Services.Auth;

public interface IJwtService
{
    string GenerateToken(ApplicationUser user, IList<string> roles);
}

public sealed class JwtService(IConfiguration configuration) : IJwtService
{
    public string GenerateToken(ApplicationUser user, IList<string> roles)
    {
        // TODO: Implement JWT generation.
        //
        // Example using System.IdentityModel.Tokens.Jwt:
        //
        //   var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Key"]!));
        //   var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        //   var claims = new List<Claim>
        //   {
        //       new(ClaimTypes.NameIdentifier, user.Id.ToString()),
        //       new(ClaimTypes.Email, user.Email!),
        //   };
        //   claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));
        //
        //   var expiry = configuration.GetValue<int>("Jwt:AccessTokenExpiryMinutes");
        //   var token = new JwtSecurityToken(
        //       issuer: configuration["Jwt:Issuer"],
        //       audience: configuration["Jwt:Audience"],
        //       claims: claims,
        //       expires: DateTime.UtcNow.AddMinutes(expiry),
        //       signingCredentials: creds);
        //
        //   return new JwtSecurityTokenHandler().WriteToken(token);

        throw new NotImplementedException(
            "Implement JWT generation in JwtService.GenerateToken. See the commented example above.");
    }
}
```

- [ ] **Step 2: Build**

```bash
dotnet build {{ProjectName}}.slnx --configuration Release
```

Expected: 0 errors, 0 warnings.

- [ ] **Step 3: Commit**

```bash
git add Shared/Services/Auth/JwtService.cs
git commit -m "Shared.Services - Add IJwtService stub with implementation guide"
```

---

## Task 6: Rewrite AuthService — wire IJwtService, implement Login

**Files:**
- Modify: `Shared/Services/Auth/AuthService.cs`

The current `AuthService` has: unused `AppDbContext` parameter, a placeholder JWT string in `Register`, and an unimplemented `Login`. Fix all three.

- [ ] **Step 1: Replace AuthService**

Replace `Shared/Services/Auth/AuthService.cs` with:

```csharp
using Microsoft.AspNetCore.Identity;
using Shared.Resources.HTTP.Auth.POST;

namespace Shared.Services.Auth;

public interface IAuthService
{
    Task<(ApplicationUser User, string Token)> Register(PostAuthRegisterRequest request, CancellationToken ct);
    Task<(ApplicationUser User, string Token)> Login(PostAuthLoginRequest request, CancellationToken ct);
}

public sealed class AuthService(
    UserManager<ApplicationUser> userManager,
    IJwtService jwtService,
    ILogger<AuthService> log) : IAuthService
{
    public async Task<(ApplicationUser User, string Token)> Register(PostAuthRegisterRequest request, CancellationToken ct)
    {
        var existing = await userManager.FindByEmailAsync(request.Email).ConfigureAwait(false);
        if (existing is not null)
            throw new InvalidOperationException("Email is already registered.");

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        var result = await userManager.CreateAsync(user, request.Password).ConfigureAwait(false);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            log.LogError("Registration failed for {Email}: {Errors}", request.Email, errors);
            throw new InvalidOperationException($"Registration failed: {errors}");
        }

        await userManager.AddToRoleAsync(user, AppRoles.User).ConfigureAwait(false);
        var roles = await userManager.GetRolesAsync(user).ConfigureAwait(false);
        var token = jwtService.GenerateToken(user, roles);

        return (user, token);
    }

    public async Task<(ApplicationUser User, string Token)> Login(PostAuthLoginRequest request, CancellationToken ct)
    {
        var user = await userManager.FindByEmailAsync(request.Email).ConfigureAwait(false);
        if (user is null || !user.IsActive)
            throw new UnauthorizedAccessException("Invalid credentials.");

        var valid = await userManager.CheckPasswordAsync(user, request.Password).ConfigureAwait(false);
        if (!valid)
            throw new UnauthorizedAccessException("Invalid credentials.");

        user.LastLoginAt = DateTime.UtcNow;
        await userManager.UpdateAsync(user).ConfigureAwait(false);

        var roles = await userManager.GetRolesAsync(user).ConfigureAwait(false);
        var token = jwtService.GenerateToken(user, roles);

        return (user, token);
    }
}
```

- [ ] **Step 2: Build**

```bash
dotnet build {{ProjectName}}.slnx --configuration Release
```

Expected: 0 errors, 0 warnings.

- [ ] **Step 3: Commit**

```bash
git add Shared/Services/Auth/AuthService.cs
git commit -m "Shared.Services - Implement AuthService.Login; wire IJwtService into Register + Login"
```

---

## Task 7: Create AuthenticatedController base class

**Files:**
- Create: `Applications/Api/Authorization/AuthenticatedController.cs`

- [ ] **Step 1: Create AuthenticatedController**

Create `Applications/Api/Authorization/AuthenticatedController.cs`:

```csharp
using System.Security.Claims;

namespace Api.Authorization;

[Authorize]
public abstract class AuthenticatedController : ControllerBase
{
    protected string CurrentUserId =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new UnauthorizedAccessException("User identity not found.");
}
```

- [ ] **Step 2: Build**

```bash
dotnet build {{ProjectName}}.slnx --configuration Release
```

Expected: 0 errors, 0 warnings.

- [ ] **Step 3: Commit**

```bash
git add Applications/Api/Authorization/AuthenticatedController.cs
git commit -m "Api - Add AuthenticatedController base class with CurrentUserId helper"
```

---

## Task 8: Fix AuthController — populate roles in GetMe

**Files:**
- Modify: `Applications/Api/Controllers/Auth/AuthController.cs`

`GetMe` currently ignores roles. Add `UserManager.GetRolesAsync` and attach via `with` expression. Also use `IList` for roles signature.

- [ ] **Step 1: Update AuthController**

Replace `Applications/Api/Controllers/Auth/AuthController.cs` with:

```csharp
using Api.Authorization;
using Databases.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Shared.Resources.HTTP.Auth.GET;
using Shared.Resources.HTTP.Auth.POST;
using Shared.Resources.HTTP.Common;
using Shared.Mapping.Auth;
using Shared.Services.Auth;

namespace Api.Controllers.Auth;

[ApiController]
[ApiVersion("1.0")]
[Tags(OpenApiTagNames.Auth)]
[Route("api/auth")]
[Produces("application/json")]
public class AuthController(IAuthService authService, IAuthMapper authMapper, UserManager<ApplicationUser> userManager) : ControllerBase
{
    [HttpPost("register")]
    [ProducesResponseType(typeof(ApiResponse<GetMe>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<GetMe>>> PostRegister(
        [FromBody] PostAuthRegisterRequest request,
        CancellationToken ct)
    {
        var (user, token) = await authService.Register(request, ct);
        var me = authMapper.ToGetMe(user);
        return CreatedAtAction(nameof(GetMe), ApiResponse<GetMe>.Created(me, token));
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(ApiResponse<GetMe>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<GetMe>>> PostLogin(
        [FromBody] PostAuthLoginRequest request,
        CancellationToken ct)
    {
        var (user, token) = await authService.Login(request, ct);
        var roles = await userManager.GetRolesAsync(user);
        var me = authMapper.ToGetMe(user) with { Roles = roles.ToList() };
        return Ok(ApiResponse<GetMe>.Success(me, token));
    }

    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<GetMe>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<GetMe>>> GetMe(CancellationToken ct)
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null)
            throw new UnauthorizedAccessException("User not found.");

        var roles = await userManager.GetRolesAsync(user);
        var me = authMapper.ToGetMe(user) with { Roles = roles.ToList() };
        return Ok(ApiResponse<GetMe>.Success(me));
    }
}
```

- [ ] **Step 2: Build**

```bash
dotnet build {{ProjectName}}.slnx --configuration Release
```

Expected: 0 errors, 0 warnings.

- [ ] **Step 3: Commit**

```bash
git add Applications/Api/Controllers/Auth/AuthController.cs
git commit -m "Api - Populate roles in GetMe and PostLogin responses"
```

---

## Task 9: Seed roles in SeedExtensions

**Files:**
- Modify: `Applications/Api/Extensions/SeedExtensions.cs`

- [ ] **Step 1: Update SeedExtensions to seed roles**

Replace `Applications/Api/Extensions/SeedExtensions.cs` with:

```csharp
using Databases.Core;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Shared.Resources.Auth;

namespace Api.Extensions;

public static class SeedExtensions
{
    public static async Task SeedDatabase(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        try
        {
            var canConnect = await db.Database.CanConnectAsync().ConfigureAwait(false);
            if (!canConnect) return;

            await db.Database.MigrateAsync().ConfigureAwait(false);

            await SeedRoles(roleManager).ConfigureAwait(false);

            // TODO: Seed default admin user from configuration
            // var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            // await SeedAdminUser(userManager, roleManager, app.Configuration).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Database seeding skipped — DB may not be ready yet.");
        }
    }

    private static async Task SeedRoles(RoleManager<ApplicationRole> roleManager)
    {
        string[] roles = [AppRoles.SuperAdmin, AppRoles.User];

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role).ConfigureAwait(false))
                await roleManager.CreateAsync(new ApplicationRole { Name = role }).ConfigureAwait(false);
        }
    }
}
```

- [ ] **Step 2: Build**

```bash
dotnet build {{ProjectName}}.slnx --configuration Release
```

Expected: 0 errors, 0 warnings.

- [ ] **Step 3: Commit**

```bash
git add Applications/Api/Extensions/SeedExtensions.cs
git commit -m "Api - Seed SuperAdmin and User roles on startup"
```

---

## Task 10: Fix and expand convention + architecture tests

**Files:**
- Modify: `Tests/Shared.Tests/ConventionTests.cs`
- Create: `Tests/Api.Tests/ConventionTests.cs`
- Modify: `Tests/Shared.Tests/GlobalUsings.cs`

The existing `ConventionTests.cs` in `Shared.Tests` references `Api.Controllers.Auth.AuthController` — a type not in any project referenced by `Shared.Tests`. Move controller-layer tests to `Api.Tests`. Fix regex. Add three new architecture tests.

- [ ] **Step 1: Replace Shared.Tests/ConventionTests.cs**

Replace `Tests/Shared.Tests/ConventionTests.cs` with:

```csharp
namespace Shared.Tests;

public sealed class ConventionTests
{
    // All test names follow MethodName_Scenario_ExpectedResult (3 segments, digits allowed).
    // Convention tests use Subject_Scope_Constraint.

    [Fact]
    public void Services_Concrete_AreSealed()
    {
        var serviceAssembly = typeof(Shared.Services.Auth.AuthService).Assembly;
        var serviceTypes = serviceAssembly.GetTypes()
            .Where(t => t.Name.EndsWith("Service") && t is { IsClass: true, IsAbstract: false })
            .ToList();

        foreach (var type in serviceTypes)
            Assert.True(type.IsSealed, $"{type.Name} in {type.Namespace} must be sealed.");
    }

    [Fact]
    public void Services_PublicMethods_HaveNoAsyncSuffix()
    {
        var serviceAssembly = typeof(Shared.Services.Auth.AuthService).Assembly;
        var serviceTypes = serviceAssembly.GetTypes()
            .Where(t => t.Name.EndsWith("Service") && t.IsClass);

        foreach (var type in serviceTypes)
        {
            var methods = type.GetMethods().Where(m => m.IsPublic && !m.IsSpecialName);
            foreach (var method in methods)
                Assert.False(method.Name.EndsWith("Async"),
                    $"{type.Name}.{method.Name} must not end with 'Async'.");
        }
    }

    [Fact]
    public void HttpModels_All_AreRecords()
    {
        var resourceAssembly = typeof(Shared.Resources.HTTP.Common.ApiResponse).Assembly;
        var httpTypes = resourceAssembly.GetTypes()
            .Where(t => t.Namespace?.Contains("HTTP") == true && t is { IsClass: true, IsAbstract: false })
            .ToList();

        foreach (var type in httpTypes)
            Assert.True(type.GetMethod("<Clone>$") is not null,
                $"{type.Name} in {type.Namespace} must be a record type.");
    }

    [Fact]
    public void HttpModels_All_HaveNoDtoSuffix()
    {
        var resourceAssembly = typeof(Shared.Resources.HTTP.Common.ApiResponse).Assembly;
        var httpTypes = resourceAssembly.GetTypes()
            .Where(t => t.Namespace?.Contains("HTTP") == true);

        foreach (var type in httpTypes)
            Assert.False(type.Name.EndsWith("Dto"),
                $"{type.Name} must not use 'Dto' suffix.");
    }

    [Fact]
    public void Mappers_Concrete_AreSealed()
    {
        var mappingAssembly = typeof(Shared.Mapping.Auth.AuthMapper).Assembly;
        var mapperTypes = mappingAssembly.GetTypes()
            .Where(t => t.Name.EndsWith("Mapper") && t is { IsClass: true, IsAbstract: false });

        foreach (var type in mapperTypes)
            Assert.True(type.IsSealed, $"{type.Name} in {type.Namespace} must be sealed.");
    }

    [Fact]
    public void Entities_All_AreNotSealed()
    {
        var coreAssembly = typeof(Databases.Core.Entities.ApplicationUser).Assembly;
        var entityTypes = coreAssembly.GetTypes()
            .Where(t => t.Namespace?.Contains("Entities") == true && t.IsClass);

        foreach (var type in entityTypes)
            Assert.False(type.IsSealed,
                $"{type.Name} must not be sealed — EF Core proxies require open entity types.");
    }

    [Fact]
    public void TestNaming_AllMethods_FollowConvention()
    {
        var testAssembly = typeof(ConventionTests).Assembly;
        var testMethods = testAssembly.GetTypes()
            .SelectMany(t => t.GetMethods())
            .Where(m => m.GetCustomAttributes(typeof(FactAttribute), true).Length > 0
                     || m.GetCustomAttributes(typeof(Xunit.TheoryAttribute), true).Length > 0);

        foreach (var method in testMethods)
            Assert.Matches(@"^[A-Z]\w+_[A-Z]\w+_[A-Z]\w+$", method.Name);
    }
}
```

- [ ] **Step 2: Create Api.Tests/ConventionTests.cs**

Create `Tests/Api.Tests/ConventionTests.cs`:

```csharp
namespace Api.Tests;

public sealed class ConventionTests
{
    [Fact]
    public void Controllers_All_AreNotSealed()
    {
        var apiAssembly = typeof(Api.Controllers.Auth.AuthController).Assembly;
        var controllerTypes = apiAssembly.GetTypes()
            .Where(t => t.Name.EndsWith("Controller") && t.IsClass && !t.IsAbstract);

        foreach (var type in controllerTypes)
            Assert.False(type.IsSealed, $"{type.Name} must not be sealed.");
    }

    [Fact]
    public void Controllers_All_HaveNoTryCatch()
    {
        var apiAssembly = typeof(Api.Controllers.Auth.AuthController).Assembly;
        var controllerTypes = apiAssembly.GetTypes()
            .Where(t => t.Name.EndsWith("Controller") && !t.IsAbstract);

        foreach (var type in controllerTypes)
        {
            var sourceCode = File.ReadAllText(GetSourceFilePath(type));
            var tryCount = System.Text.RegularExpressions.Regex.Matches(sourceCode, @"\btry\s*\{").Count;
            Assert.True(tryCount == 0,
                $"{type.Name} contains {tryCount} try block(s). Controllers must not use try/catch.");
        }
    }

    [Fact]
    public void TestNaming_AllMethods_FollowConvention()
    {
        var testAssembly = typeof(ConventionTests).Assembly;
        var testMethods = testAssembly.GetTypes()
            .SelectMany(t => t.GetMethods())
            .Where(m => m.GetCustomAttributes(typeof(FactAttribute), true).Length > 0
                     || m.GetCustomAttributes(typeof(Xunit.TheoryAttribute), true).Length > 0);

        foreach (var method in testMethods)
            Assert.Matches(@"^[A-Z]\w+_[A-Z]\w+_[A-Z]\w+$", method.Name);
    }

    private static string GetSourceFilePath(Type type)
    {
        // Assembly "Api" lives at Applications/Api/ — strip assembly name prefix from namespace
        // to get the relative path within the project.
        var basePath = AppDomain.CurrentDomain.BaseDirectory;
        var repoRoot = Path.GetFullPath(Path.Combine(basePath, "..", "..", "..", ".."));
        var assemblyName = type.Assembly.GetName().Name!;
        var relativePath = type.FullName!
            .Replace(assemblyName + ".", string.Empty)
            .Replace('.', '/');
        return Path.Combine(repoRoot, "Applications", assemblyName, relativePath + ".cs");
    }
}
```

- [ ] **Step 3: Update Shared.Tests/GlobalUsings.cs**

Replace `Tests/Shared.Tests/GlobalUsings.cs` with:

```csharp
global using Databases.Core.Entities;
global using Microsoft.AspNetCore.Identity;
global using Microsoft.EntityFrameworkCore;
global using Microsoft.Extensions.Logging.Abstractions;
global using Moq;
global using Shared.Resources.Auth;
global using Shared.Resources.HTTP.Auth.POST;
global using Shared.Services.Auth;
global using Xunit;
```

- [ ] **Step 4: Run convention tests**

```bash
dotnet test Tests/Shared.Tests/Shared.Tests.csproj --configuration Release
dotnet test Tests/Api.Tests/Api.Tests.csproj --configuration Release
```

Expected: All tests pass. If `Controllers_NoTryCatch` fails on a source file path issue, verify the relative path calculation matches your build output directory depth.

- [ ] **Step 5: Commit**

```bash
git add Tests/Shared.Tests/ConventionTests.cs Tests/Api.Tests/ConventionTests.cs Tests/Shared.Tests/GlobalUsings.cs
git commit -m "Tests - Fix convention tests; add Entities_NeverSealed, Mappers_AreSealed, Controllers_NeverSealed"
```

---

## Task 11: Implement AuthServiceTests with Moq

**Files:**
- Modify: `Tests/Shared.Tests/AuthServiceTests.cs`

- [ ] **Step 1: Replace AuthServiceTests**

Replace `Tests/Shared.Tests/AuthServiceTests.cs` with:

```csharp
// All usings come from GlobalUsings.cs — no explicit usings needed here.
namespace Shared.Tests;

public sealed class AuthServiceTests
{
    private static Mock<UserManager<ApplicationUser>> CreateUserManagerMock()
    {
        var store = new Mock<IUserStore<ApplicationUser>>();
        return new Mock<UserManager<ApplicationUser>>(
            store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
    }

    [Fact]
    public async Task Register_WithValidRequest_ReturnsUserAndToken()
    {
        // Arrange
        var userManagerMock = CreateUserManagerMock();
        var jwtServiceMock = new Mock<IJwtService>();

        userManagerMock
            .Setup(m => m.FindByEmailAsync("new@example.com"))
            .ReturnsAsync((ApplicationUser?)null);

        userManagerMock
            .Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), "Password123!"))
            .ReturnsAsync(IdentityResult.Success);

        userManagerMock
            .Setup(m => m.AddToRoleAsync(It.IsAny<ApplicationUser>(), AppRoles.User))
            .ReturnsAsync(IdentityResult.Success);

        userManagerMock
            .Setup(m => m.GetRolesAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(new List<string> { AppRoles.User });

        jwtServiceMock
            .Setup(j => j.GenerateToken(It.IsAny<ApplicationUser>(), It.IsAny<IList<string>>()))
            .Returns("stub-token");

        var service = new AuthService(
            userManagerMock.Object,
            jwtServiceMock.Object,
            NullLogger<AuthService>.Instance);

        var request = new PostAuthRegisterRequest
        {
            Email = "new@example.com",
            Password = "Password123!",
            ConfirmPassword = "Password123!"
        };

        // Act
        var (user, token) = await service.Register(request, CancellationToken.None);

        // Assert
        Assert.Equal("new@example.com", user.Email);
        Assert.Equal("stub-token", token);
        userManagerMock.Verify(m => m.AddToRoleAsync(It.IsAny<ApplicationUser>(), AppRoles.User), Times.Once);
    }

    [Fact]
    public async Task Register_WithExistingEmail_ThrowsInvalidOperationException()
    {
        // Arrange
        var userManagerMock = CreateUserManagerMock();
        var jwtServiceMock = new Mock<IJwtService>();

        userManagerMock
            .Setup(m => m.FindByEmailAsync("taken@example.com"))
            .ReturnsAsync(new ApplicationUser { Email = "taken@example.com" });

        var service = new AuthService(
            userManagerMock.Object,
            jwtServiceMock.Object,
            NullLogger<AuthService>.Instance);

        var request = new PostAuthRegisterRequest
        {
            Email = "taken@example.com",
            Password = "Password123!",
            ConfirmPassword = "Password123!"
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.Register(request, CancellationToken.None));
    }

    [Fact]
    public async Task Login_WithInvalidPassword_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var userManagerMock = CreateUserManagerMock();
        var jwtServiceMock = new Mock<IJwtService>();

        var existingUser = new ApplicationUser { Email = "user@example.com", IsActive = true };

        userManagerMock
            .Setup(m => m.FindByEmailAsync("user@example.com"))
            .ReturnsAsync(existingUser);

        userManagerMock
            .Setup(m => m.CheckPasswordAsync(existingUser, "WrongPassword!"))
            .ReturnsAsync(false);

        var service = new AuthService(
            userManagerMock.Object,
            jwtServiceMock.Object,
            NullLogger<AuthService>.Instance);

        var request = new PostAuthLoginRequest
        {
            Email = "user@example.com",
            Password = "WrongPassword!"
        };

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => service.Login(request, CancellationToken.None));
    }
}
```

- [ ] **Step 2: Run the tests**

```bash
dotnet test Tests/Shared.Tests/Shared.Tests.csproj --configuration Release
```

Expected: All tests pass. The `Register_WithValidRequest_ReturnsUserAndToken` test will only pass if `IJwtService.GenerateToken` is mocked — it won't call the real stub's `NotImplementedException`.

- [ ] **Step 3: Commit**

```bash
git add Tests/Shared.Tests/AuthServiceTests.cs
git commit -m "Tests.Shared - Implement AuthServiceTests with Moq"
```

---

## Task 12: Fix WebAppFactory and AuthControllerTests

**Files:**
- Modify: `Tests/Api.Tests/WebAppFactory.cs`
- Modify: `Tests/Api.Tests/AuthControllerTests.cs`

- [ ] **Step 1: Update WebAppFactory — ensure Identity tables exist**

Replace `Tests/Api.Tests/WebAppFactory.cs` with:

```csharp
using Microsoft.AspNetCore.Mvc.Testing;

namespace Api.Tests;

public sealed class WebAppFactory : WebApplicationFactory<Api.Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<Databases.Core.AppDbContext>));
            if (descriptor is not null) services.Remove(descriptor);

            services.AddDbContext<Databases.Core.AppDbContext>(options =>
                options.UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}"));
        });

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "test-secret-key-minimum-16-characters-long",
                ["Jwt:Issuer"] = "test-issuer",
                ["Jwt:Audience"] = "test-audience",
                ["Jwt:AccessTokenExpiryMinutes"] = "60"
            });
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
    }
}
```

- [ ] **Step 2: Update AuthControllerTests — assert response body shape**

Replace `Tests/Api.Tests/AuthControllerTests.cs` with:

```csharp
using System.Net.Http.Json;
using Shared.Resources.HTTP.Common;

namespace Api.Tests;

public sealed class AuthControllerTests : IClassFixture<WebAppFactory>
{
    private readonly HttpClient _client;

    public AuthControllerTests(WebAppFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetHealth_WhenCalled_Returns200()
    {
        var response = await _client.GetAsync("/api/health");
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Healthy", content);
    }

    [Fact]
    public async Task GetAuthMe_WithoutToken_Returns401()
    {
        var response = await _client.GetAsync("/api/auth/me");
        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PostRegister_WithInvalidBody_Returns400WithError()
    {
        var content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json");

        var response = await _client.PostAsync("/api/auth/register", content);

        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse>();
        Assert.NotNull(body);
        Assert.False(body.Success);
        Assert.NotNull(body.Error);
    }
}
```

- [ ] **Step 3: Run Api.Tests**

```bash
dotnet test Tests/Api.Tests/Api.Tests.csproj --configuration Release
```

Expected: All 3 tests pass. (`PostRegister_WithInvalidRequest_Returns400WithErrorBody` passes because FluentValidation rejects the empty `{}` request.)

- [ ] **Step 4: Commit**

```bash
git add Tests/Api.Tests/WebAppFactory.cs Tests/Api.Tests/AuthControllerTests.cs
git commit -m "Tests.Api - Fix WebAppFactory; improve AuthControllerTests response body assertions"
```

---

## Task 13: Rewrite AGENTS.md

**Files:**
- Modify: `AGENTS.md`

Remove all Blazor sections. Add `JwtService`, `AuthenticatedController`, `GetMe` pattern sections.

- [ ] **Step 1: Replace AGENTS.md**

Replace `AGENTS.md` with:

```markdown
# {{ProjectName}} — Architecture, Conventions & Agent Guide

## Overview

{{ProjectDescription}} Built with .NET 10.

**Architecture:** Single deployable — ASP.NET Core 10 Web API hosting REST endpoints, SignalR, Quartz jobs, and OpenAPI/Scalar docs.

## Solution Structure

```
{{ProjectName}}/
├── Applications/
│   └── Api/                          # ASP.NET Core Web API
│       ├── Authorization/            # AppPermissions, AuthenticatedController
│       ├── Controllers/              # Organized by bounded context
│       ├── Extensions/               # Service registration, middleware pipeline, seeding
│       ├── Middleware/               # ExceptionMiddleware
│       ├── OpenApi/                  # Scalar config, document transformers
│       └── Data/Migrations/          # EF Core migrations
├── Databases/
│   ├── Core/                         # AppDbContext, Entities, Enums
│   └── ...per-context config projects
├── Shared/
│   ├── Resources/                    # HTTP models (records), validators, enums, AppRoles
│   ├── Services/                     # Business logic services — sealed, primary ctors
│   ├── Mapping/                      # AutoMapper wrappers — one per context
│   └── Jobs/                         # Quartz.NET background jobs
└── Tests/
    ├── Api.Tests/                    # Integration tests (WebApplicationFactory + InMemory)
    └── Shared.Tests/                 # Unit + convention/architecture tests
```

## Bounded Contexts

All code is organized by business context across every layer:

- Controller: `Applications/Api/Controllers/{Context}/`
- Service: `Shared/Services/{Context}/`
- HTTP models: `Shared/Resources/HTTP/{Context}/{Verb}/`
- EF config: `Databases/{Context}/`
- Mapper: `Shared/Mapping/{Context}/`

## Code Conventions

### General C#

- **File-scoped namespaces** — `namespace X.Y.Z;` always, never block-scoped
- **Primary constructors** on services, controllers, jobs, auth handlers, mappers
- **No primary constructors** on entities (use `{ get; set; }` properties)
- **Interface + implementation in one file** — named after the implementation, interface at top
- **No `Async` suffix** — `GetUser`, not `GetUserAsync`
- **No `Dto` suffix** — `GetUser`, not `UserDto`
- **`GlobalUsings.cs`** per project — entity aliases, framework-level imports
- **`TreatWarningsAsErrors=true`** — zero warnings always

### `sealed` Rules

| Layer | Sealed? |
|-------|---------|
| Services | Always sealed |
| Mappers | Always sealed |
| Jobs | Always sealed |
| Auth handlers | Always sealed |
| Controllers | **Never sealed** |
| Entities | **Never sealed** — EF Core proxies require open types |

### HTTP Models

- All models are `record`, never `class`
- Read models: `Get{Resource}` (e.g., `GetUser`)
- Write models: `{Verb}{Resource}Request` (e.g., `PostUserRequest`)
- Organized in `HTTP/{Context}/{Verb}/` subfolders
- Shared envelope: `HTTP/Common/ApiResponse.cs`

### API Controllers

- One controller per resource per context
- Route: `api/{context}/{resource}` (kebab-case, plural collections)
- Action names: `<HttpVerb><Resource>` — `GetUser`, `PostCompany`, `DeleteBooking`
- Return `ApiResponse<T>` always
- **Authenticated feature controllers** inherit `AuthenticatedController` — exposes `CurrentUserId`
- `AuthController` does NOT inherit it (register/login are anonymous; `GetMe` uses its own `[Authorize]`)
- **No try/catch** — `ExceptionMiddleware` maps exceptions: `KeyNotFoundException`→404, `InvalidOperationException`→400, `UnauthorizedAccessException`→401
- Explicit binding attributes: `[FromBody]`, `[FromQuery]`, `[FromRoute]`
- `[ProducesResponseType]` on every action
- `[Tags(OpenApiTagNames.X)]` — never string literals

### Services

- Interface + implementation same file, named after implementation
- Primary constructors
- Method names mirror controllers: `<HttpVerb><Resource>`
- `ConfigureAwait(false)` on every `await` in library code
- `CancellationToken ct` last parameter on every async method
- Every `catch` block must log — no silent swallowing
- No `async void`

### IJwtService — Stub Pattern

`Shared/Services/Auth/JwtService.cs` contains the stub:

```csharp
public string GenerateToken(ApplicationUser user, IList<string> roles)
{
    // TODO: implement using JwtSecurityTokenHandler
    throw new NotImplementedException("...");
}
```

**To implement:** Add `System.IdentityModel.Tokens.Jwt` package, follow the commented example in the file. Register via `services.AddScoped<IJwtService, JwtService>()` (already done in `ServiceExtensions`).

### AuthenticatedController

```csharp
// All authenticated feature controllers:
public class CompanyController(ICompanyService companyService) : AuthenticatedController
{
    // CurrentUserId is available here
}
```

`AuthController` is the exception — it inherits `ControllerBase` directly because register/login are public.

### Roles Population in GetMe

Roles are NOT mapped by AutoMapper (`.ForMember(d => d.Roles, o => o.Ignore())`). They are loaded and attached in the controller:

```csharp
var roles = await userManager.GetRolesAsync(user);
var me = authMapper.ToGetMe(user) with { Roles = roles.ToList() };
```

### Validators

- FluentValidation, auto-discovered via `AddValidatorsFromAssemblyContaining`
- One validator per request model
- `Validators/{Context}/` subfolders
- `sealed` classes

### Mapping

- `IAuthMapper` → `AuthMapper(IMapper mapper)` — sealed wrapper per context
- AutoMapper profile per context: `AuthMappingProfile`
- Registered: `services.AddScoped<IAuthMapper, AuthMapper>()`

### EF Core

- Singular table names
- Per-context schemas: `Auth.User`, `Company.Service`
- Enum properties: `HasConversion<string>()`
- `.AsSplitQuery()` for multiple collection includes
- `.Select()` projection for read-only queries
- Decimal precision always specified
- Constraint naming: `PK-Schema_Table_Column`, `FK-Schema_Table_Src_Dst`, `IX-Schema_Table_Column`

### Entity PK Pattern

```csharp
public Guid UserId { get; set; }
[NotMapped]
public Guid Id { get => UserId; set => UserId = value; }
```

### AppRoles

Role name constants live in `Shared/Resources/Auth/AppRoles.cs` (accessible from both `Api` and `Shared.Services`). Permission policy constants live in `Api/Authorization/AppPermissions.cs`.

## EF Core Migrations

```bash
dotnet ef migrations add <Name> \
  --project Applications/Api \
  --startup-project Applications/Api \
  --output-dir Data/Migrations
```

Migrations auto-applied on startup via `MigrateAsync()` in `SeedExtensions`.

## Running the Project

```bash
docker compose -f docker-compose.devdb.yml up -d
dotnet build
dotnet run --project Applications/Api   # → http://localhost:5050
open http://localhost:5050/docs/v1      # Scalar
dotnet test
```

## Test Conventions

- xUnit, test naming: `MethodName_Scenario_ExpectedResult`
- One test class per service/controller
- Convention tests enforce architecture rules — run in CI on every push
- Integration tests use `WebApplicationFactory` + InMemory DB
- Unit tests use Moq — never instantiate real `UserManager<T>` in unit tests

## Git Commit Convention

Format: `Project - What was done`

```
Api - Added company endpoint
Shared.Services - Added CompanyService
Databases.Core - Added Company entity
```
```

- [ ] **Step 2: Build (docs-only change, just verify)**

```bash
dotnet build {{ProjectName}}.slnx --configuration Release
```

- [ ] **Step 3: Commit**

```bash
git add AGENTS.md
git commit -m "AGENTS.md - Remove Blazor sections; add JwtService stub, AuthenticatedController, roles pattern"
```

---

## Task 14: Update CLAUDE.md and copilot-instructions.md

**Files:**
- Modify: `CLAUDE.md`
- Modify: `.github/copilot-instructions.md`

- [ ] **Step 1: Replace CLAUDE.md**

Replace `CLAUDE.md` with:

```markdown
# {{ProjectName}}

{{ProjectDescription}} Built with .NET 10.

Full conventions in `AGENTS.md`. Read it before making non-trivial changes.

## Stack at a glance

| Layer | Tech |
|-------|------|
| API | ASP.NET Core 10 |
| DB | SQL Server / PostgreSQL, EF Core 10 |
| Auth | ASP.NET Identity + JWT (stub — implement `IJwtService`) |
| Jobs | Quartz.NET |
| Validation | FluentValidation |
| Mapping | AutoMapper |
| API Docs | Scalar |
| Tests | xUnit + WebApplicationFactory + Moq |
| Solution | `.slnx` |

## Run

```bash
docker compose -f docker-compose.devdb.yml up -d
dotnet run --project Applications/Api       # :5050
dotnet test
```

Dev URLs: `:5050` (API), `:5050/docs/v1` (Scalar).

## Hard rules

- File-scoped namespaces. Namespace = folder path.
- Primary constructors on services/controllers/jobs. Never on entities.
- `sealed` services/jobs/mappers/auth-handlers. Never `sealed` controllers or entities.
- No `Async` suffix. No `Dto` suffix. No try/catch in controllers.
- `ConfigureAwait(false)` on every await in libraries. `CancellationToken ct` last.
- `TreatWarningsAsErrors=true`.
- HTTP models are `record`, not `class`.
- Throw from services; `ExceptionMiddleware` maps to HTTP.
- Authenticated feature controllers inherit `AuthenticatedController`.
- `AppRoles` lives in `Shared.Resources.Auth` — accessible from both Api and Services.

## EF migrations

```bash
dotnet ef migrations add <Name> \
  --project Applications/Api \
  --startup-project Applications/Api \
  --output-dir Data/Migrations
```

## Commit style

`Project - What was done`. Single scope. No body for small changes.
```

- [ ] **Step 2: Replace .github/copilot-instructions.md**

Replace `.github/copilot-instructions.md` with:

```markdown
You are working on {{ProjectName}}, a .NET 10 ASP.NET Core Web API.

Read AGENTS.md at the repo root before making non-trivial changes.

## Repository structure

- `Applications/Api` — main host: controllers, middleware, OpenAPI/Scalar, auth, seeding
- `Shared/Resources` — HTTP models (records), validators, enums, AppRoles
- `Shared/Services` — business logic services by bounded context
- `Shared/Mapping` — AutoMapper mapping profiles per context
- `Shared/Jobs` — Quartz jobs
- `Databases/Core` — EF Core entities, DbContext
- `Databases/{Context}` — EF IEntityTypeConfiguration per bounded context
- `Tests/Api.Tests` — integration tests (WebApplicationFactory)
- `Tests/Shared.Tests` — unit + convention/architecture tests

## Key conventions

- Primary constructors on services, controllers, jobs — never on entities
- `sealed` services/jobs/mappers — never sealed controllers or entities
- No `Async` suffix, no `Dto` suffix
- HTTP models are `record` types always
- No try/catch in controllers — ExceptionMiddleware handles all exceptions
- All authenticated feature controllers inherit `AuthenticatedController`
- `AppRoles` constants are in `Shared.Resources.Auth` (accessible from Services)
- JWT generation is stubbed in `IJwtService` — implement `GenerateToken` per project

## Validation commands

```bash
dotnet restore {{ProjectName}}.slnx
dotnet build {{ProjectName}}.slnx --configuration Release
dotnet test Tests/Api.Tests/Api.Tests.csproj --configuration Release
dotnet test Tests/Shared.Tests/Shared.Tests.csproj --configuration Release
```

## Git conventions

Format: `Project - What was done`  
Example: `Api - Added company endpoint`
```

- [ ] **Step 3: Commit**

```bash
git add CLAUDE.md .github/copilot-instructions.md
git commit -m "Docs - Update CLAUDE.md and copilot-instructions to remove Blazor, add new patterns"
```

---

## Task 15: Fix CI workflow

**Files:**
- Modify: `.github/workflows/ci.yml`

Remove Node.js setup, npm install, and Web.Tests test step.

- [ ] **Step 1: Replace ci.yml**

Replace `.github/workflows/ci.yml` with:

```yaml
name: Build & Test

on:
  push:
    branches:
      - main
      - dev
  pull_request:
    branches:
      - main
      - dev

concurrency:
  group: ci-${{ github.workflow }}-${{ github.ref }}
  cancel-in-progress: true

permissions:
  contents: read

jobs:
  build-test:
    runs-on: ubuntu-latest
    timeout-minutes: 30

    steps:
      - name: Checkout
        uses: actions/checkout@v4

      - name: Setup .NET 10
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'

      - name: Cache NuGet packages
        uses: actions/cache@v4
        with:
          path: ~/.nuget/packages
          key: ${{ runner.os }}-nuget-${{ hashFiles('**/Directory.Packages.props', '**/*.csproj') }}
          restore-keys: ${{ runner.os }}-nuget-

      - name: Restore solution
        run: dotnet restore {{ProjectName}}.slnx

      - name: Check formatting
        run: dotnet format --verify-no-changes --verbosity diagnostic

      - name: Build solution
        run: dotnet build {{ProjectName}}.slnx --configuration Release --no-restore

      - name: Test API project
        run: >
          dotnet test Tests/Api.Tests/Api.Tests.csproj
          --configuration Release --no-build
          --collect "XPlat Code Coverage" --settings coverlet.runsettings
          --results-directory TestResults/
          --logger "trx;LogFileName=api-tests.trx"

      - name: Test shared services
        run: >
          dotnet test Tests/Shared.Tests/Shared.Tests.csproj
          --configuration Release --no-build
          --collect "XPlat Code Coverage" --settings coverlet.runsettings
          --results-directory TestResults/
          --logger "trx;LogFileName=shared-tests.trx"

      - name: Upload test results
        if: always()
        uses: actions/upload-artifact@v4
        with:
          name: test-results
          path: TestResults/
          if-no-files-found: warn

      - name: Upload coverage to Codecov
        if: always()
        uses: codecov/codecov-action@v5
        with:
          directory: TestResults/
          fail_ci_if_error: false
```

- [ ] **Step 2: Commit**

```bash
git add .github/workflows/ci.yml
git commit -m "CI - Remove Node/Web steps; fix upload-artifact to v4"
```

---

## Task 16: Full build and test verification

- [ ] **Step 1: Full clean build**

```bash
dotnet build {{ProjectName}}.slnx --configuration Release
```

Expected: 0 errors, 0 warnings.

- [ ] **Step 2: Run all tests**

```bash
dotnet test {{ProjectName}}.slnx --configuration Release
```

Expected: All tests in `Api.Tests` and `Shared.Tests` pass.

- [ ] **Step 3: Verify convention tests in detail**

```bash
dotnet test Tests/Shared.Tests/Shared.Tests.csproj --configuration Release --verbosity normal
dotnet test Tests/Api.Tests/Api.Tests.csproj --configuration Release --verbosity normal
```

Confirm all of these pass:
- `Services_Concrete_AreSealed`
- `Services_PublicMethods_HaveNoAsyncSuffix`
- `HttpModels_All_AreRecords`
- `HttpModels_All_HaveNoDtoSuffix`
- `Mappers_Concrete_AreSealed`
- `Entities_All_AreNotSealed`
- `TestNaming_AllMethods_FollowConvention` (both projects)
- `Controllers_All_AreNotSealed`
- `Controllers_All_HaveNoTryCatch`

- [ ] **Step 4: Final commit**

```bash
git add -A
git commit -m "Template - Full polish complete; all conventions enforced, tests passing"
```
