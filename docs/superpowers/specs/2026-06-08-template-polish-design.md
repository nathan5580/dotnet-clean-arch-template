# Template Polish — Design Spec
**Date:** 2026-06-08  
**Status:** ⚠️ SUPERSEDED (2026-06-14) — its core premise (remove Blazor) was reversed; Blazor is kept, co-hosted. Convention fixes here live on in the `2026-06-14-*` specs (foundation + auth vertical). Retained for reference only.

## Goal

Make `dotnet-clean-arch-template` a fully professional, convention-correct reference template for any new .NET 10 project. Pure API architecture — no Blazor frontend.

## Out of scope

- Real JWT generation (stubs show the pattern; implementer fills in)
- Refresh token persistence
- Password reset flow
- Frontend / Blazor (removed entirely)

---

## 1. Architecture

### Final structure

```
{{ProjectName}}/
├── Applications/Api/          # ASP.NET Core 10 Web API — sole deployable
│   ├── Authorization/         # AppPermissions, AppRoles, AuthenticatedController
│   ├── Controllers/Auth/      # AuthController
│   ├── Extensions/            # ServiceExtensions, AuthExtensions, SeedExtensions
│   ├── Middleware/            # ExceptionMiddleware
│   └── OpenApi/               # OpenApiDocumentTransformer, OpenApiTagNames
├── Databases/
│   ├── Core/                  # AppDbContext, entities (ApplicationUser, ApplicationRole, UserActionAudit), enums
│   └── Auth/                  # EF IEntityTypeConfiguration per entity
├── Shared/
│   ├── Resources/             # HTTP models (records), validators, enums
│   ├── Services/Auth/         # IAuthService + AuthService, IJwtService + JwtService (stub)
│   ├── Mapping/Auth/          # IAuthMapper + AuthMapper + AuthMappingProfile
│   └── Jobs/                  # QuartzExtensions (example job commented)
└── Tests/
    ├── Api.Tests/             # Integration — WebApplicationFactory + InMemory
    └── Shared.Tests/          # Unit (Moq) + convention/architecture tests
```

### Removed

- `Applications/Web/` — entire Blazor WASM project
- `Tests/Web.Tests/` — Blazor test project
- All Blazor/WASM NuGet packages
- `UseBlazorFrameworkFiles`, `UseWebAssemblyDebugging`, `MapFallbackToFile` from pipeline
- All Blazor/i18n/CSS sections from AGENTS.md and CLAUDE.md

---

## 2. Convention fixes

Every rule stated in AGENTS.md that the current code violates:

| Location | Violation | Fix |
|---|---|---|
| `AuthController` | `sealed` class | Remove `sealed` — controllers never sealed |
| `ApplicationUser` | `sealed` class | Remove `sealed` — entities never sealed |
| `ApplicationRole` | `sealed` class | Remove `sealed` — entities never sealed |
| `ExceptionMiddleware.WriteErrorResponse` | Returns anonymous `new { Error, StatusCode }` | Return `ApiResponse.Fail(message, statusCode)` serialized as JSON |
| `ApiClient` (removed with Web) | N/A | Removed with frontend |
| `LocalizationService` (removed with Web) | N/A | Removed with frontend |
| `ConventionTests` naming regex | `^[A-Z][a-zA-Z]+_...` rejects digits | Allow `\w` in final segment so `_Returns200`, `_Returns401` pass |

---

## 3. Auth scaffolding (stub pattern)

### IJwtService / JwtService

New file: `Shared/Services/Auth/JwtService.cs`

```
public interface IJwtService
{
    string GenerateToken(ApplicationUser user, IList<string> roles);
}

public sealed class JwtService(IConfiguration configuration) : IJwtService
{
    public string GenerateToken(ApplicationUser user, IList<string> roles)
    {
        // TODO: implement JWT generation using System.IdentityModel.Tokens.Jwt
        // Key: configuration["Jwt:Key"]
        // Issuer: configuration["Jwt:Issuer"]
        // Audience: configuration["Jwt:Audience"]
        // Expiry: configuration.GetValue<int>("Jwt:AccessTokenExpiryMinutes")
        throw new NotImplementedException("Replace with JWT generation using JwtSecurityTokenHandler.");
    }
}
```

### AuthService

- `Register`: creates user via `UserManager`, assigns `AppRoles.User` role, calls `_jwtService.GenerateToken(user, roles)` — fully wired, only the JWT itself is stubbed.
- `Login`: checks password via `UserManager.CheckPasswordAsync`, throws `UnauthorizedAccessException` on failure, calls `_jwtService.GenerateToken(user, roles)`.
- `GetMe` stays in `AuthController` using `UserManager.GetUserAsync(User)` + `UserManager.GetRolesAsync(user)`. Roles are attached via `with` expression after mapping. `AuthController` does NOT inherit `AuthenticatedController` (register/login are anonymous); `GetMe` carries its own `[Authorize]` attribute.

### AuthenticatedController

New file: `Applications/Api/Authorization/AuthenticatedController.cs`

```
[Authorize]
public abstract class AuthenticatedController : ControllerBase
{
    protected string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new UnauthorizedAccessException("User identity not found.");
}
```

All future feature controllers inherit this. `AuthController` does NOT — its endpoints are public.

### GetMe — roles populated

`AuthMappingProfile` currently has `.ForMember(d => d.Roles, o => o.Ignore())`. Roles are loaded in `AuthController.GetMe` via `UserManager.GetRolesAsync(user)` and set on the returned `GetMe` record using `with` expression. The mapper maps all other fields; roles are added after mapping.

---

## 4. Seed

`SeedExtensions.cs` seeds idempotently:
1. Create `AppRoles.SuperAdmin` role if missing
2. Create `AppRoles.User` role if missing
3. (Comment stub) Create default admin user — implementer fills in credentials from config

---

## 5. Tests

### AuthServiceTests (`Shared.Tests`)

Both tests fully implemented using Moq:
- `Register_WithValidRequest_ReturnsUserAndToken` — mocks `UserManager`, `IJwtService`; asserts returned user email and non-null token
- `Register_WithExistingEmail_ThrowsInvalidOperationException` — sets up existing user in mock, asserts throws

### AuthMapperTests (`Shared.Tests`)

New test class:
- `ToGetMe_MapsAllFields_Correctly` — constructs `ApplicationUser`, maps via `AuthMapper`, asserts `UserId`, `Email`, `CreatedAt`, `IsActive`

### ConventionTests (`Shared.Tests`)

Additions:
- `Mappers_AreSealed` — all `*Mapper` classes in Mapping assembly are sealed
- `Controllers_NeverSealed` — no controller in Api assembly is sealed
- `Entities_NeverSealed` — no entity in Core assembly is sealed
- Fix `TestNaming_FollowsConvention` regex to `^[A-Z]\w+_[A-Z]\w+_[A-Z]\w+$`

### WebAppFactory (`Api.Tests`)

Call `db.Database.EnsureCreatedAsync()` after replacing with InMemory — Identity tables must exist for integration tests.

### AuthControllerTests (`Api.Tests`)

Assert response body shape on existing tests:
- `PostRegister_WithInvalidRequest_Returns400` — deserialize body, assert `success: false`
- `GetAuthMe_WithoutToken_Returns401` — assert response code only (correct as-is)

---

## 6. AGENTS.md

Add / update sections:
- Remove all Blazor/i18n/CSS sections
- Add `IJwtService` stub convention: where the TODO is, what to fill in
- Add `AuthenticatedController` usage: "all authenticated feature controllers inherit this"
- Add `GetMe` pattern: how roles are attached post-mapping
- Tighten `sealed` table to match what's actually in the codebase
- Fix solution structure diagram to reflect no Web project

## CLAUDE.md

- Remove Blazor stack row from table
- Remove Blazor hard rules section
- Update run commands (no Web project)
- Update solution structure

## .github/copilot-instructions.md

- Remove Web/frontend references

---

## 7. CI/CD

| Fix | |
|---|---|
| `actions/setup-node@v6` | Remove entirely — no frontend, no Node needed |
| Remove `Install frontend dependencies` step | No `package.json` |
| Remove `Test Web project` step | No Web.Tests |
| Keep Codecov with `fail_ci_if_error: false` | Fine as-is |
| `pr-title-lint.yml` vs AGENTS.md commit format | Add comment in pr-title-lint: enforces PR title only; AGENTS.md format is for local commits |

---

## 8. Directory.Packages.props

Remove Blazor-related packages:
- `Microsoft.AspNetCore.Components.Authorization`
- `Microsoft.AspNetCore.Components.WebAssembly`
- `Microsoft.AspNetCore.Components.WebAssembly.DevServer`
- `Microsoft.AspNetCore.Components.WebAssembly.Server`

Keep all backend packages as-is.
