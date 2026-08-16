<p align="center">
  <img src="https://img.shields.io/badge/.NET-10.0-512bd4?logo=dotnet" alt=".NET 10" />
  <img src="https://img.shields.io/badge/Blazor-WASM-512bd4?logo=blazor" alt="Blazor WASM" />
  <img src="https://img.shields.io/badge/license-MIT-green" alt="MIT License" />
  <img src="https://img.shields.io/badge/prs-welcome-brightgreen" alt="PRs Welcome" />
</p>

<h1 align="center">dotnet-clean-arch-template</h1>

<p align="center">
  <strong>Production-grade .NET 10 clean architecture template.</strong><br />
  Co-hosted Blazor WASM · Bounded contexts · Convention tests · AI-agent ready.
</p>

<br />

> **Not a toy.** The architecture, conventions, and tooling here are distilled from two live SaaS products (multi-tenant isolation, payments, RBAC, GDPR). The template ships a minimal-but-complete auth vertical slice plus an **executable convention suite** that fails CI on drift. Zero warnings, zero guesswork.

---

## Architecture

```
{{ProjectName}}/
├── Applications/
│   ├── Api/                          # ASP.NET Core 10 — controllers, middleware, SignalR, Quartz
│   └── Web/                          # Blazor WASM 10 — Tailwind v4, i18n, co-hosted
├── Databases/
│   ├── Core/                         # AppDbContext, starter entities, PK pattern, enums
│   ├── Auth/  Catalog/  …per-context  # EF Core IEntityTypeConfiguration<T> per bounded context
├── Shared/
│   ├── Resources/                    # HTTP models (records), FluentValidation validators, enums
│   ├── Services/                     # Business logic — sealed, primary ctors, verb-first
│   ├── Mapping/                      # Mapperly source-generated mappers — one per context
│   └── Jobs/                         # Quartz.NET background jobs
└── Tests/
    ├── Api.Tests/                    # Integration — WebApplicationFactory + InMemory DB
    ├── Architecture.Tests/           # NetArchTest + Roslyn — conventions enforced as tests
    ├── Shared.Tests/                 # Unit tests (Moq)
    └── Web.Tests/                    # Blazor infrastructure tests (stub)
```

**The API co-hosts everything** — REST endpoints, Blazor WASM static files, OpenAPI/Scalar docs, SignalR hubs, and Quartz jobs. One deployable. The Web project runs standalone for UI dev with hot reload.

**Bounded contexts** cut through every layer vertically — change a feature and the controller, service, models, validation, mapping, and DB config all live under the same context directory.

---

## What's Inside

### Build & infrastructure
- `.slnx` solution (modern XML format) — 13 projects organized in 4 folders
- `Directory.Build.props` — net10.0, Nullable, TreatWarningsAsErrors
- `Directory.Packages.props` — **central package management**, 40+ NuGet packages pinned
- `.editorconfig` — naming rules (`_underscore` fields, `I` prefix interfaces), var preferences, code styles
- `coverlet.runsettings` — Cobertura/JSON/OpenCover output, module exclusions
- `nuget.config` — single source (`nuget.org`)
- `.gitignore` / `.dockerignore` — production-hardened exclusions

### CI/CD (GitHub Actions)
- `ci.yml` — restore → format check → build → 3 test projects → coverage → Codecov
- `pr-title-lint.yml` — conventional commit enforcement (`feat`, `fix`, `deps`, …)
- `dependabot.yml` — grouped updates: NuGet (5 groups), GitHub Actions, npm (Tailwind group)
- `labeler.yml` — auto-label PRs by changed path (api, frontend, tests, security, ci, dependencies)
- **PR template** — bounded context, validation checklist, UI evidence, deployment notes
- **Issue templates** — bug report + feature request, both with bounded context fields

### Agent instructions (AI coding assistants)
- `AGENTS.md` — **the canonical source of truth**. Opens with a "For AI Agents — Start Here" section (commands, the end-to-end vertical-slice recipe, self-check), then every convention, plus an "Extending the Template" guide and instantiation steps. Read by Claude, Copilot, Cursor, and any agent-aware tool.
- `CONVENTIONS.md` — one-page cheat-sheet of the hard rules
- `CLAUDE.md` — compact Claude-specific brief with hard rules, run commands, stack summary
- `.github/copilot-instructions.md` — GitHub Copilot workspace instructions

### Backend skeleton — demonstrating every convention

| Pattern | Example | Convention |
|---|---|---|
| Controller | `AuthController.cs` | Primary ctor, `ApiResponse<T>`, `[FromBody]`, `[ProducesResponseType]`, `CancellationToken ct`, no try/catch |
| Service | `IAuthService` + `AuthService` in one file | Sealed, primary ctor, `ConfigureAwait(false)`, verb-first |
| HTTP model | `GetMe`, `PostAuthRegisterRequest` | `record`, no Dto suffix, `Get*` / `{Verb}*Request` |
| Validator | `PostAuthRegisterRequestValidator` | FluentValidation, auto-discovered, one per model |
| Mapper | `IAuthMapper` → `[Mapper] sealed partial AuthMapper` | Mapperly source generator — no runtime dependency |
| Middleware | `ExceptionMiddleware.cs` | KeyNotFound→404, InvalidOp→400, Unauthorized→401 |
| DB config | `UserConfiguration` | Per-context assembly, constraint naming, HasConversion\<string\> |
| Entity | `UserActionAudit` | `AuditId` + `[NotMapped] Id` alias pattern |
| GlobalUsings | Per-project files | Entity type aliases, no `System.*` duplicates |

### Frontend skeleton — Blazor WASM + Tailwind v4

| Component | Convention |
|---|---|
| `AppPageFrame` | Page shell with Title/Subtitle, Narrow/Medium/Wide variants |
| `AppLoader` | Spinner + text for loading states |
| `MetaPanel` | Content card with Kicker/Title/Description, Error state + Retry, Empty state |
| `RedirectToLogin` | Standard unauthorized redirect |
| `ApiClient` | Typed `IApiClient` with `ApiResponse<T>` unwrapping |
| `ToastService` | Success/Error notifications via event delegates |
| `ThemeService` | Brand color application |
| `LocalizationService` | Runtime JSON loading, `T(key, args)`, English fallback |
| `MainLayout` | CascadingAuthenticationState + @Body |

### Convention tests — the rules enforce themselves

`Tests/Architecture.Tests` (NetArchTest + Roslyn) fails CI on any violation:

```csharp
Services_Concrete_AreSealed                   // services/mappers/jobs/validators/handlers sealed
Controllers_Concrete_AreNotSealed             // controllers + entities never sealed
HttpModels_All_AreRecords                     // every HTTP type is a record; no Dto suffix
Controllers_Contain_NoTryCatch                // zero try/catch in controllers
LibraryAwaits_Use_ConfigureAwaitFalse         // ConfigureAwait(false) across Shared.*/Databases.*
MethodBodies_Separate_LogicalStages           // blank line between logical stages; none at the braces
AllFiles_Use_FileScopedNamespaces             // file-scoped namespaces only
TestMethods_All_FollowSubjectScenarioExpected // Method_Scenario_Expected naming
```

These run in CI at every push. They catch violations before code review.

---

## Quick Start

```bash
# 1. Create your project from the template — pick ONE:
#    A. dotnet new (recommended)
dotnet new install .
dotnet new cleanarch -n MySaaS --description "My SaaS product." -o ../MySaaS && cd ../MySaaS
#    B. clone + setup script (replaces {{ProjectName}}/{{ProjectDescription}}, renames the .slnx)
# git clone https://github.com/nathan5580/dotnet-clean-arch-template.git MySaaS && cd MySaaS
# ./setup.sh MySaaS "My SaaS product."                            # macOS/Linux
# pwsh ./setup.ps1 -Name MySaaS -Description "My SaaS product."   # Windows

# 2. Start dev database
docker compose -f docker-compose.devdb.yml up -d

# 3. Build (0 errors, 0 warnings)
dotnet build

# 4. Run
dotnet run --project Applications/Api        # → http://localhost:5050
dotnet run --project Applications/Web        # → http://localhost:5129 (standalone)

# 5. API docs
open http://localhost:5050/docs/v1           # Scalar UI

# 6. Tests
dotnet test                                  # 4 test projects, convention tests included
```

---

## Adding Your First Bounded Context

> The template ships a complete **Products** (`Catalog`) context as a worked example — entity + shared enum, EF config, records, validators, Mapperly mapper, `ProductService` CRUD, an authed `ProductsController`, Blazor list/detail/create pages, a migration, and tests. Copy it as your starting point; the steps below are the same shape.

```
1. Databases/Core/Entities/YourEntity.cs           Entity with PK pattern
2. Databases/YourContext/YourConfig.cs             IEntityTypeConfiguration<T>
3. Shared/Resources/HTTP/YourContext/GET/          GetYourResource records
4. Shared/Resources/HTTP/YourContext/POST/         PostYourResourceRequest records
5. Shared/Resources/Validators/YourContext/        FluentValidation validators
6. Shared/Services/YourContext/YourService.cs      IYourService + sealed impl
7. Shared/Mapping/YourContext/YourMapper.cs        [Mapper] sealed partial (Mapperly)
8. Applications/Api/Controllers/YourContext/       YourController.cs
9. Applications/Api/Extensions/ServiceExtensions   Register DI
10. Applications/Web/Pages/YourContext/            Blazor pages + code-behind
11. Tests/                                         xUnit tests
```

Every step follows the same verb-first naming, same file-scoped namespace, same sealed-or-not rule. No decision fatigue.

---

## Why These Conventions?

| Convention | Why it matters |
|---|---|
| No `Async` suffix | C# Task-returning methods are inherently async; the suffix is noise. Every method in the codebase is async-callable without the clutter. |
| No `Dto` suffix | DTO is an implementation detail, not a communication contract. `GetBooking` communicates intent. `BookingDto` communicates nothing. |
| `sealed` services | JIT can devirtualize sealed class method calls. Free performance. Services don't need inheritance. |
| `.ConfigureAwait(false)` | Defends against deadlocks when called from non-ASP.NET contexts. Costs nothing, prevents black-box debugging sessions. |
| No try/catch in controllers | Single `ExceptionMiddleware` maps exceptions to HTTP status codes. Controllers stay clean. No duplicated error-handling logic. |
| `record` for models | Value equality, `with` expressions, positional construction. Exactly what immutable DTOs need. |
| Interface + impl same file | Find the interface, the implementation is right there. No hunting across files. Named after the concrete type. |
| `CancellationToken ct` last | Async pipeline cancellation from client through to database. One parameter name. Always last. |
| `ApiResponse<T>` envelope | One consistent response shape. Frontend unwraps the same way everywhere. |
| Singular table names | Consistency with EF Core conventions. `User` reads better than `Users` in every query. |
| PK = `UserId` alias `Id` | Explicit naming in queries (`x.UserId`), clean `x.Id` in generic code. Both worlds. |
| `GlobalUsings` per project | No shared global state. Entity aliases (`UserEntity`) prevent namespace collisions between context assemblies. |
| Convention tests in CI | Code review catches intent. Automation catches drift. Both are necessary. |

---

## Tech Stack

| Layer | Technology |
|---|---|
| Backend | ASP.NET Core 10 Web API |
| Frontend | Blazor WebAssembly 10 + Tailwind CSS v4 |
| Database | SQL Server via EF Core 10 (swap to PostgreSQL with 2 lines) |
| Auth | ASP.NET Identity + JWT |
| Validation | FluentValidation (auto-discovered) |
| Mapping | Mapperly (source generator, per context) |
| Jobs | Quartz.NET |
| API Docs | Scalar |
| Logging | Serilog (console + file) |
| Testing | xUnit + WebApplicationFactory + InMemory DB + Moq |
| Coverage | coverlet (Cobertura + JSON + OpenCover) |
| Solution | `.slnx` (modern XML) |
| Package mgmt | Centralized (Directory.Packages.props) |

---

## License

MIT — use it, fork it, ship products with it.

---

<p align="center">
  <sub>Built from patterns proven in production SaaS. No abstractions that weren't earned.</sub>
</p>
