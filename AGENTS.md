# {{ProjectName}} — Architecture, Conventions & Guide

## Overview

{{ProjectDescription}} Built with .NET 10.

**Architecture:** ASP.NET Core 10 Web API (`Api`) that co-hosts a Blazor WebAssembly 10 client (`Web`). The API hosts everything — Controllers, SignalR, Quartz jobs, OpenAPI/Scalar docs, and Blazor WASM static files. The Web project can also run standalone for UI development.

## Solution Structure

```
{{ProjectName}}/
├── Applications/
│   ├── Api/                          # ASP.NET Core Web API (co-hosts Blazor WASM)
│   │   ├── Authorization/            # Custom auth handlers, policies, resource filters
│   │   ├── Controllers/              # Organized by bounded context subfolder
│   │   ├── Extensions/               # Service registration, middleware pipeline, seeding
│   │   ├── Middleware/                # Exception, Auth middleware
│   │   ├── OpenApi/                   # Scalar config, document/operation/schema transformers
│   │   └── Data/Migrations/           # EF Core migrations
│   └── Web/                           # Blazor WebAssembly client
│       ├── Components/                # Layout, State, Surface components
│       ├── Layout/                    # MainLayout, NavMenu
│       ├── Pages/                     # Organized by bounded context
│       ├── Services/                  # JwtAuthStateProvider, ApiClient, ToastService
│       ├── Styles/                    # app.css (Tailwind v4 input)
│       └── wwwroot/                   # Static assets + i18n locale files
├── Databases/
│   ├── Core/                          # EF Core — AppDbContext, Entities, Enums
│   ├── Auth/                          # Identity tables EF config
│   └── ...per-context database projects
├── Shared/
│   ├── Resources/                     # HTTP models (records), enums, FluentValidation validators
│   ├── Services/                      # Business logic services, organized by context
│   ├── Mapping/                       # AutoMapper-backed wrappers — one per context
│   └── Jobs/                          # Quartz.NET background jobs
└── Tests/
    ├── Api.Tests/                     # Integration tests (WebApplicationFactory)
    ├── Shared.Tests/                  # Unit + convention tests
    └── Web.Tests/                     # Blazor convention tests
```

## Bounded Contexts

All code is organized by business context across every layer. Each context maps to:

- A controller subdirectory in `Applications/Api/Controllers/{Context}/`
- A service subdirectory in `Shared/Services/{Context}/`
- HTTP model subdirectory in `Shared/Resources/HTTP/{Context}/`
- An EF Core config project in `Databases/{Context}/`
- (Optionally) a mapping wrapper in `Shared/Mapping/{Context}/`

## Code Conventions

### General C# Conventions

- **No top-level statements** — all `Program.cs` use explicit `Main` method
- **File-scoped namespaces** — every file uses `namespace X.Y.Z;` (C# 10+), never block-scoped
- **Primary constructors by layer:**
  - Services, controllers, jobs, authorization handlers — always use primary constructors
  - Mappers — use primary constructors (wrapping AutoMapper's `IMapper`)
  - AutoMapper profiles — use explicit constructors (required by the library)
  - Entities — never use primary constructors; use `{ get; set; }` property pattern
- **Interface + implementation in one file** — interface at the top, file named after implementation
  - E.g., `AuthService.cs` contains both `IAuthService` and `AuthService`
- **No `Async` suffix** — `GetUser`, not `GetUserAsync`
- **No `Dto` suffix** — verb-first naming: `GetUser`, `PostUserRequest`
- **GlobalUsings.cs** — each project has its own with type aliases for entities and framework-level usings
- **KISS** — keep it simple; don't over-abstract
- **Comments** — only when needed for understanding
- **Namespaces** — follow folder structure

### `sealed` Convention

| Layer | Sealed? | Why |
|-------|---------|-----|
| Services | Always sealed | JIT devirtualization; no inheritance needed |
| Mappers | Always sealed | No override scenario |
| Jobs | Always sealed | Quartz jobs are instantiated directly |
| Auth handlers | Always sealed | Single-purpose |
| Controllers | Never sealed | May need mocking in tests |
| Entities | Never sealed | EF Core proxies require open types |

### HTTP Model Conventions

- All models are `record` types, never `class`
- Read models: `Get{Resource}` (e.g., `GetUser`, `GetCompany`)
- Write models: `{Verb}{Resource}Request` (e.g., `PostUserRequest`, `PutCompanyRequest`)
- Organized in `HTTP/{Context}/{Verb}/` subfolders
- Shared envelope: `HTTP/Common/ApiResponse.cs`

### API Controller Conventions

- **One controller per resource per context**
- Route pattern: `api/{context}/{resource}` (kebab-case, plural for collections)
- Action naming: `<HttpVerb><Resource>` (e.g., `GetUser`, `PostCompany`, `PutCompany`, `DeleteCompany`)
- Return `ApiResponse<T>` for consistency
- **All authenticated controllers** inherit from `AuthenticatedController` base class
- **No try/catch** — global `ExceptionMiddleware` maps: `KeyNotFoundException`→404, `InvalidOperationException`→400, `UnauthorizedAccessException`→401
- **Never inject `AppDbContext` directly** — always wrap with a service layer
- Controllers inject **mapper interfaces** (`IAuthMapper`, etc.)
- **Explicit binding attributes** on all action parameters (`[FromBody]`, `[FromQuery]`, `[FromRoute]`)
- **`[ProducesResponseType]` on every action**
- **OpenAPI tags via constants** — use `OpenApiTagNames.{Resource}`, never string literals

### Service Conventions

- Interface + implementation in same file, named after implementation
- Primary constructors for DI
- Method naming mirrors controllers: `<HttpVerb><Resource>`
- **`ConfigureAwait(false)`** on every `await` in library code
- **`CancellationToken ct`** propagated through all async methods (last parameter)
- **Every `catch` block must log** — never silent swallowing
- **No `async void`** — use `async Task` always

### Validator Conventions

- FluentValidation with auto-discovery via `services.AddValidatorsFromAssemblyContaining<...>()`
- One validator per request model
- Organized by context in `Validators/{Context}/`
- File naming: `{Model}Validator.cs`

### Mapping Conventions

- **AutoMapper**-based wrapper interfaces per context: `IAuthMapper` → `AuthMapper(IMapper mapper)`
- AutoMapper profiles per context: `AuthMappingProfile`
- Registered as `services.AddScoped<IAuthMapper, AuthMapper>()`

### EF Core Conventions

- **Singular** table names
- **Per-context schemas**: `Auth.User`, `Company.Service`
- Enum properties stored as strings with `HasConversion<string>()`
- `.AsSplitQuery()` for queries with multiple collection includes
- `.Select()` projection for read-only `Get*` queries
- Decimal precision always specified
- Constraint naming: `PK-Schema_Table_Column`, `FK-Schema_Table_Src_Dst`

### Entity Primary Key Pattern

```csharp
public Guid UserId { get; set; }
[NotMapped]
public Guid Id { get => UserId; set => UserId = value; }
```

## Blazor WASM Frontend Conventions

### Project Structure

```
Web/
├── Components/
│   ├── Layout/          # AppPageFrame (page shell)
│   ├── State/           # AppLoader, AppStateCard
│   └── Surface/         # MetaPanel, Toast
├── Layout/              # MainLayout, NavMenu
├── Pages/               # Organized by context: Auth/, Company/, Home/
├── Services/            # JwtAuthStateProvider, ApiClient, ThemeService, ToastService
├── Styles/              # app.css (Tailwind v4 input)
└── wwwroot/
    ├── css/
    ├── js/
    └── locales/         # One folder per language, one JSON file per namespace
```

### Blazor Page Conventions

- **All pages use code-behind** — `.razor.cs` with `[Inject]` properties (never `@inject` in `.razor`)
- **Page state patterns:**
  - Data pages: `PageState<T>` record struct with `LoadingState()`, `ErrorState(string)`, `DataState(T)`
  - Form/auth pages: `bool _isLoading`, `string? _error`, `T? _data`
- **Three rendering states** per page: loading, error, content
- HTTP calls through `IApiClient` — `GetAsync<T>`, `PostAsync<T>`, `PutAsync<T>`, `DeleteAsync<T>`
- Component naming: `App*` (layout), `Meta*` (surface/form), domain names for features

### i18n Conventions

Translations are plain JSON files fetched at runtime:

```
wwwroot/locales/
  en/
    common.json       # Shared: cancel, save, loading, error states
    auth.json         # Login, register, forgot password
    nav.json          # Navigation (loaded by MainLayout)
    ...per-view namespaces
  fr/  de/  ...
```

- Key naming: `namespace.element` (dot-separated, e.g., `auth.login.heading`)
- Namespaces are singular (`booking`, not `bookings`)
- `common.json` loaded on app start; `nav.json` / `footer.json` loaded by MainLayout
- Page-specific namespaces loaded in `OnInitializedAsync()` per page

### CSS Conventions

- **Tailwind CSS v4** via `Styles/app.css`
- Custom component classes prefixed: `meta-`, `app-`
- Theme variables in CSS custom properties (e.g., `--primary-500`, `--accent-500`)
- No Bootstrap

## Authorization Model

- Custom policy-based auth with `[VerifiedUser]` filter
- Dynamic permission handler: `[HasRight("companies.read")]`
- Resource access filters: `[ValidateCompanyAccess]`, `[ValidateUserAccess]`
- Permissions defined in `AppPermissions.cs` as string constants
- Role-to-permission mapping in `AppPermissions.ByRole` dictionary

## EF Core Migrations

Migrations live in `Applications/Api/Data/Migrations/`. Applied automatically at startup via `MigrateAsync()`.

```bash
dotnet ef migrations add <MigrationName> \
    --project Applications/Api \
    --startup-project Applications/Api \
    --output-dir Data/Migrations
```

## Running the Project

```bash
# Start dev database
docker compose -f docker-compose.devdb.yml up -d

# Build
dotnet build

# Run API (hosts Blazor WASM) — http://localhost:5050
dotnet run --project Applications/Api

# Run Web standalone (UI dev) — http://localhost:5129
dotnet run --project Applications/Web

# Run all tests
dotnet test

# API docs at /docs/v1 (Scalar)
```

## Test Conventions

- **xUnit** for .NET tests, **Playwright** for E2E, **Vitest/Jest** for JS
- Test naming: `MethodName_Scenario_ExpectedResult`
- One test file per service/controller: `<Resource>Tests.cs`
- Convention tests enforce architecture rules automatically
- Data-layer tests use `UseInMemoryDatabase()` and `NullLogger<T>.Instance`

## Git Commit Convention

Format: `Project - What has been done`

```
Api - Added user registration endpoint
Web - Added login page with validation
Databases.Core - Added User entity
{{ProjectName}} - Initial scaffold
```

## Template Usage

1. Clone this repository
2. Run `find . -type f -exec sed -i '' 's/{{ProjectName}}/YourProjectName/g' {} +`
3. Run `find . -type f -exec sed -i '' 's/{{ProjectDescription}}/Your project description./g' {} +`
4. Update `Directory.Packages.props` with your specific NuGet packages
5. Add your bounded contexts and start building
