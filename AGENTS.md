# {{ProjectName}} — Architecture, Conventions & Agent Guide

{{ProjectDescription}} Built with .NET 10 + co-hosted Blazor WebAssembly.

> This file is the **single source of truth** for how this codebase is built. `CLAUDE.md` and `.github/copilot-instructions.md` are short pointers back here. `CONVENTIONS.md` is a one-page cheat-sheet. Read this before any non-trivial change.

---

## For AI Agents — Start Here

**Your conventions are executable.** `Tests/Architecture.Tests` enforces them with NetArchTest + Roslyn. After any change, run `dotnet test` — a failing architecture test means you broke a convention, and the failure message tells you which file and rule. Treat that as the ground-truth checker, not this prose.

**Golden rules (the ones that trip agents up):**
- File-scoped namespaces; namespace = folder path.
- `sealed` on services / mappers / jobs / validators / auth-handlers. **Never** `sealed` on controllers or entities.
- Primary constructors on services / controllers / jobs / mappers. **Never** on entities.
- No `Async` suffix on method names. No `Dto` suffix on types. No `try/catch` in controllers (throw; `ExceptionMiddleware` maps it).
- HTTP models are `record`, organized `HTTP/{Context}/{Verb}/`.
- `ConfigureAwait(false)` on every `await` in library projects (`Shared.*`, `Databases.*`). `CancellationToken ct` is the last parameter.
- **Method-body aeration:** one blank line right after a method's opening `{` and one right before its closing `}` (methods only — not types or control-flow blocks). `dotnet format` will NOT add these; write them by hand.
- `TreatWarningsAsErrors=true` — the build is zero-warning.

**Commands:**
```bash
docker compose -f docker-compose.devdb.yml up -d   # dev SQL Server
dotnet build "{{ProjectName}}.slnx" -c Release      # 0 warnings, 0 errors expected
dotnet test  "{{ProjectName}}.slnx" -c Release      # all green, incl. Architecture.Tests
dotnet run --project Applications/Api               # API + co-hosted Blazor → http://localhost:5050
```
Scalar API docs: `http://localhost:5050/docs/v1`.

**Recipe — add a feature end-to-end (a "vertical slice" for context `Widget`):**
1. **Entity** — `Databases/Core/Entities/` : `public class Widget { ... }` (not sealed; PK pattern below).
2. **EF config** — `Databases/{Widget}/WidgetConfiguration.cs` : `IEntityTypeConfiguration<Widget>`, `ToTable("Widget","Widget")`, `HasConversion<string>()` for enums, named constraints. Register its assembly in `ServiceExtensions` via `AppDbContext.ExtraConfigurationAssemblies.Add(typeof(WidgetConfiguration).Assembly)` (configs outside `Databases.Core` aren't auto-discovered — see *Known Wrinkles*).
3. **HTTP models** — `Shared/Resources/HTTP/Widget/GET|POST/` : `record GetWidget`, `record PostWidgetRequest` (no `Dto`).
4. **Validator** — `Shared/Resources/Validators/Widget/` : `public sealed class PostWidgetRequestValidator : AbstractValidator<PostWidgetRequest>`.
5. **Mapper** — `Shared/Mapping/Widget/` : `[Mapper] public sealed partial class WidgetMapper : IWidgetMapper`.
6. **Service** — `Shared/Services/Widget/WidgetService.cs` : `IWidgetService` + `public sealed class WidgetService(...) : IWidgetService` (primary ctor, `ConfigureAwait(false)`, `ct` last).
7. **Controller** — `Applications/Api/Controllers/Widget/WidgetController.cs` : `public class WidgetController(...) : AuthenticatedController` (not sealed; inherits `[Authorize]` + `CurrentUserId`), returns `ApiResponse<T>`, `[ProducesResponseType]` on every action, no try/catch. (Public controllers may stay on `ControllerBase` via the allow-list — see Architecture Tests.)
8. **Register** — add `services.AddScoped<IWidgetService, WidgetService>();` and `services.AddScoped<IWidgetMapper, WidgetMapper>();` in `ServiceExtensions`.
9. **Migration** — `dotnet ef migrations add AddWidget --project Applications/Api --startup-project Applications/Api --output-dir Data/Migrations`.
10. **Tests** — unit (`Tests/Shared.Tests`) + integration (`Tests/Api.Tests`), names `Method_Scenario_Expected`.
11. **Self-check** — `dotnet test`. Green = conventions satisfied.

**Commit format:** `Project - What was done` (e.g. `Api - Add Widget endpoints`). Single scope, no body for small changes.

---

## Architecture

A single deployable: an **ASP.NET Core 10 Web API** (`Api`) that co-hosts a **Blazor WebAssembly 10** client (`Web`). The API serves REST controllers, Quartz jobs, OpenAPI/Scalar docs, and the Blazor WASM static files. `Web` can also run standalone for UI work.

## Solution Structure

```
{{ProjectName}}/
├── Applications/
│   ├── Api/                          # ASP.NET Core Web API (co-hosts Blazor WASM)
│   │   ├── Authorization/            # AppPermissions, AppRoles (string constants)
│   │   ├── Controllers/              # Organized by bounded context subfolder
│   │   ├── Extensions/               # Service registration, middleware pipeline, seeding
│   │   ├── Middleware/               # ExceptionMiddleware
│   │   ├── OpenApi/                  # Scalar config, document transformers
│   │   └── Data/Migrations/          # EF Core migrations (auto-applied at startup)
│   └── Web/                          # Blazor WebAssembly client
│       ├── Components/               # Layout, State, Surface components
│       ├── Layout/                   # MainLayout
│       ├── Pages/                    # Organized by bounded context
│       ├── Services/                 # ApiClient, ThemeService, LocalizationService, ToastService
│       ├── Styles/                   # app.css (Tailwind v4 input)
│       └── wwwroot/                  # Static assets + i18n locale files
├── Databases/
│   ├── Core/                         # AppDbContext, Entities, Enums
│   ├── Auth/                         # Identity EF configuration
│   ├── Catalog/                      # Product EF config — the worked showcase context
│   └── ...per-context config projects
├── Shared/
│   ├── Resources/                    # HTTP models (records), enums, FluentValidation validators
│   ├── Services/                     # Business logic services (sealed, primary ctors)
│   ├── Mapping/                      # Mapperly source-generated mappers — one per context
│   └── Jobs/                         # Quartz.NET background jobs
└── Tests/
    ├── Api.Tests/                    # Integration (WebApplicationFactory + InMemory)
    ├── Architecture.Tests/           # NetArchTest + Roslyn convention enforcement
    ├── Shared.Tests/                 # Unit tests (Moq)
    └── Web.Tests/                    # (stub) Blazor tests
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

- **Explicit `Main`** — `Program.cs` uses an explicit `Main` method (no top-level statements). `Program` is a non-static `public class` (so `WebApplicationFactory<Program>` works).
- **File-scoped namespaces** — `namespace X.Y.Z;`, never block-scoped (enforced).
- **Primary constructors by layer:** services / controllers / jobs / mappers / auth handlers → yes. Entities → never (use `{ get; set; }`).
- **Interface + implementation in one file**, named after the implementation (`AuthService.cs` holds `IAuthService` + `AuthService`).
- **No `Async` suffix**; **no `Dto` suffix**.
- **`GlobalUsings.cs`** per project for framework imports and entity aliases.
- **KISS**; comments only where they earn their keep.

### Method-Body Aeration

Every method with a block body gets one blank line immediately after the opening `{` and one immediately before the closing `}`. Type bodies and control-flow blocks (`if`/`for`/`try`) stay compact.

```csharp
public GetMe ToGetMe(ApplicationUser user)
{

    var roles = LoadRoles(user);
    return Map(user, roles);

}
```

Enforced by `Tests/Architecture.Tests` (Roslyn). `dotnet format` does not add these blank lines — write them by hand.

### `sealed` Convention

| Layer | Sealed? | Why |
|-------|---------|-----|
| Services | Always | JIT devirtualization; no inheritance needed |
| Mappers | Always | No override scenario |
| Jobs | Always | Quartz instantiates directly |
| Validators | Always | Single-purpose |
| Auth handlers | Always | Single-purpose |
| Controllers | **Never** | May be derived / mocked |
| Entities | **Never** | EF Core proxies require open types |

### HTTP Models

- All `record`, never `class`. Read: `Get{Resource}`. Write: `{Verb}{Resource}Request`.
- Organized in `HTTP/{Context}/{Verb}/`. Shared envelope: `HTTP/Common/ApiResponse.cs` (`ApiResponse<T>.Ok/.Created/.Fail`).

### API Controllers

- One controller per resource per context. Route `api/{context}/{resource}` (kebab-case, plural collections).
- Action naming `<HttpVerb><Resource>` (`GetUser`, `PostCompany`). Return `ApiResponse<T>`.
- **No try/catch** — `ExceptionMiddleware` maps `KeyNotFoundException`→404, `InvalidOperationException`→400, `UnauthorizedAccessException`→401, anything else→500.
- **Never inject `AppDbContext` directly** — go through a service. Inject mapper interfaces (`IAuthMapper`).
- Explicit binding attributes (`[FromBody]`/`[FromQuery]`/`[FromRoute]`), `[ProducesResponseType]` on every action, OpenAPI tags via `OpenApiTagNames.{X}` constants (no string literals).
- Authenticated feature controllers inherit **`AuthenticatedController`** (an `[Authorize]` base exposing `CurrentUserId`) — enforced by an Architecture.Tests rule. The bundled `AuthController` inherits `ControllerBase` directly (register/login are public; `GetMe` carries its own `[Authorize]`) and is the allow-listed exception.

### Services

- Interface + impl in one file. Primary constructors. Method names mirror controllers.
- `ConfigureAwait(false)` on every library `await`. `CancellationToken ct` last. Every `catch` logs. No `async void`.

### Validators

- FluentValidation, auto-discovered via `AddValidatorsFromAssemblyContaining<...>()`. One `sealed` validator per request model, in `Validators/{Context}/`.

### Mapping — Mapperly

- Source generator, no runtime mapping dependency. Wrapper interface per context.
- `[Mapper] public sealed partial class AuthMapper : IAuthMapper` with `public partial GetMe ToGetMe(...)`.
- Renames/ignores via `[MapProperty(nameof(Src.X), nameof(Dst.Y))]` and `[MapperIgnoreTarget(nameof(Dst.Z))]`.
- Registered `services.AddScoped<IAuthMapper, AuthMapper>()` (plain classes — no container).

### EF Core

- Singular table names; per-context schemas (`Auth.User`, `Catalog.Product`). Enum properties `HasConversion<string>()` (DB stores the name).
- **Domain enums** live in `Shared/Resources/Enums` so both entities (`Databases.Core` references `Shared.Resources`) and HTTP records share one type; JSON serializes them as names via `JsonStringEnumConverter` (registered in `ServiceExtensions`). The `Catalog`/`Product` context is the worked end-to-end example of the whole recipe.
- `.AsSplitQuery()` for multiple collection includes; `.Select()` projections for read queries; decimal precision always specified.
- Constraint naming: `PK-Schema_Table_Column`, `FK-Schema_Table_Src_Dst`, `IX-Schema_Table_Column`.

### Entity Primary Key Pattern

```csharp
public Guid WidgetId { get; set; }
[NotMapped]
public Guid Id { get => WidgetId; set => WidgetId = value; }
```
(For Identity-derived entities whose base already declares `Id`, use `public new Guid Id { ... }`.)

## Blazor WASM Frontend Conventions

```
Web/
├── Components/   # Layout (AppPageFrame), State (AppLoader), Surface (MetaPanel)
├── Layout/       # MainLayout
├── Pages/        # by context: Auth/, Home/
├── Services/     # ApiClient, ThemeService, LocalizationService, ToastService
├── Styles/       # app.css (Tailwind v4 input)
└── wwwroot/locales/  # one folder per language, one JSON file per namespace
```

- **Code-behind** `.razor.cs` for all pages; inject via `[Inject]` in code-behind, never `@inject` in `.razor`.
- **Three render states** per page: loading / error / content.
- HTTP through `IApiClient` (`GetAsync<T>`, `PostAsync<T>`, ...).
- **Tailwind CSS v4** via `Styles/app.css`; custom classes prefixed `app-`/`meta-`; theme via CSS variables. No Bootstrap.
- **i18n**: JSON files in `wwwroot/locales/{lang}/{namespace}.json`; keys `namespace.element`; singular namespaces; page namespaces loaded in `OnInitializedAsync()`. Ships with `en` (add languages by adding sibling folders).

## EF Core Migrations

Migrations live in `Applications/Api/Data/Migrations/` and are applied automatically at startup via `MigrateAsync()` in `SeedExtensions` (skipped if the DB can't be reached).

```bash
dotnet ef migrations add <Name> \
  --project Applications/Api --startup-project Applications/Api \
  --output-dir Data/Migrations
```

## Running the Project

```bash
docker compose -f docker-compose.devdb.yml up -d
dotnet run --project Applications/Api    # API + co-hosted Blazor → http://localhost:5050
dotnet run --project Applications/Web    # Blazor standalone (UI dev) → http://localhost:5129
dotnet test "{{ProjectName}}.slnx"       # all suites
```

## Architecture Tests

`Tests/Architecture.Tests` makes conventions executable — CI fails on violation. **When you add a convention, add a rule here.**

- **Structural (NetArchTest):** services/mappers/jobs/validators/handlers sealed; controllers + entities not sealed; HTTP models are records; no `Dto` suffix; `ct` named/last; enum properties use `.HasConversion<string>()`; layering (`Databases.*`/`Shared.*` never depend on `Api`).
- **Source (Roslyn):** no try/catch in controllers; no `async void`; `ConfigureAwait(false)` on every library await; file-scoped namespaces; method-body aeration.
- **Naming:** test methods match `Subject_Scenario_Expected`.

## Test Conventions

- **xUnit** only. Test naming `MethodName_Scenario_ExpectedResult` (three segments; digits allowed). One test class per service/controller.
- Integration tests use `WebApplicationFactory` + `UseInMemoryDatabase`; unit tests use Moq and `NullLogger<T>.Instance`.

## Git Commit Convention

`Project - What was done` (single scope, no body for small changes):

```
Api - Add Widget endpoints
Shared.Mapping - Replace AutoMapper with Mapperly
Databases.Core - Add Widget entity
{{ProjectName}} - Initial scaffold
```

---

## Extending the Template

The **auth vertical is fully implemented** — use it as the worked example. The patterns after it are what production apps add next; they are **not in the template yet** — wire them in when you need them.

### Implemented auth (reference)

- **JWT generation** — `Shared/Services/Auth/JwtService.cs` (`IJwtService`) signs an HMAC-SHA256 token with NameIdentifier/Email/Role claims and a configurable expiry; `AuthService.Register`/`Login` call it. JWT validation is wired in `AuthExtensions`, which **fails fast at startup** if `Jwt:Key` is missing/blank/<32 bytes. The shipped `appsettings.json` leaves `Jwt:Key` empty — supply it in prod via env var / secret store; `appsettings.Development.json` ships a clearly-marked dev-only key so `dotnet run` works.
- **`AuthenticatedController`** — `Applications/Api/Authorization/AuthenticatedController.cs`: an `[Authorize]` base exposing `CurrentUserId`. Feature controllers inherit it (enforced by an Architecture.Tests rule); `AuthController` inherits `ControllerBase` and is the allow-listed public exception.
- **Roles** — constants in `Shared/Resources/Auth/AppRoles.cs` (`SuperAdmin`, `User`), seeded idempotently on startup by `SeedExtensions`.

### Richer authorization (permissions/policies)

The scaffold ships `AppPermissions` (string constants like `users.read`). Production apps extend this with a dynamic permission policy (`[HasRight(AppPermissions.X)]`), a verified-user filter (`[VerifiedUser]`), resource-access filters (`[ValidateWidgetAccess]`), and an `AppPermissions.ByRole` map. Add these under `Applications/Api/Authorization/` and enforce "no raw role strings in `[Authorize(Roles=...)]`" with an Architecture.Tests rule.

### Default admin & refresh tokens

Seed a default admin user from configuration in `SeedExtensions`, and add refresh-token issuance/persistence/revocation (the config has `RefreshTokenExpiryDays` but no refresh flow yet).

---

## Using This Template

Replace the `{{ProjectName}}` and `{{ProjectDescription}}` tokens (and rename `{{ProjectName}}.slnx`). Three ways — pick one:

**A. `dotnet new` (recommended for CLI / agents):**
```bash
dotnet new install .
dotnet new cleanarch -n MyApp --description "My project description." -o ../MyApp
```
Replaces all tokens, renames the `.slnx`, and omits internal planning docs.

**B. Setup script (after a plain clone):**
```bash
./setup.sh MyApp "My project description."        # macOS / Linux
pwsh ./setup.ps1 -Name MyApp -Description "My project description."   # Windows / cross-platform
```
Replaces tokens in tracked text files and renames the `.slnx`.

**C. Manual:** replace `{{ProjectName}}` and `{{ProjectDescription}}` across tracked text files and rename `{{ProjectName}}.slnx` yourself.

Then: `dotnet build`, add your bounded contexts (see the recipe above), and start building.
</content>
