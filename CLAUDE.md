# {{ProjectName}}

{{ProjectDescription}} Built with .NET 10 + Blazor WASM.

Full conventions in `AGENTS.md` (start with its "For AI Agents — Start Here" section). One-page cheat-sheet: `CONVENTIONS.md`. Read before non-trivial changes.

## Stack at a glance

| Layer | Tech |
|-------|------|
| API | ASP.NET Core 10 |
| Frontend | Blazor WebAssembly 10 + Tailwind CSS v4 |
| DB | SQL Server / PostgreSQL, EF Core 10 |
| Auth | ASP.NET Identity + JWT |
| Jobs | Quartz.NET |
| Validation | FluentValidation |
| Mapping | Mapperly |
| API Docs | Scalar |
| Tests | xUnit + WebApplicationFactory + Moq |
| Solution | `.slnx` |

## Run

```bash
docker compose -f docker-compose.devdb.yml up -d
dotnet run --project Applications/Api       # :5050
dotnet run --project Applications/Web       # :5129 (standalone UI dev)
dotnet test
```

Dev URLs: `:5050` (API), `:5050/docs/v1` (Scalar).

## New project from this template

```bash
dotnet new install . && dotnet new cleanarch -n MyApp --description "..."   # or: ./setup.sh MyApp "..."
```

Replaces `{{ProjectName}}`/`{{ProjectDescription}}` and renames `{{ProjectName}}.slnx`.

## Hard rules

C#:
- File-scoped namespaces. Namespace = folder path.
- Primary constructors on services/controllers/jobs. Never on entities.
- `sealed` services/jobs/mappers/auth-handlers. Never `sealed` controllers or entities.
- No `Async` suffix. No `Dto` suffix. No try/catch in controllers.
- `ConfigureAwait(false)` on every await in libraries. `CancellationToken ct` last.
- `TreatWarningsAsErrors=true`.
- HTTP models are `record`, not `class`.
- Throw from services; `ExceptionMiddleware` maps to HTTP.
- Method bodies aerated: blank line between logical stages, none at the braces (methods only).
- Conventions enforced by `Tests/Architecture.Tests` (NetArchTest + Roslyn) — keep it green.

Blazor:
- Code-behind `.razor.cs` for all pages.
- `[Inject]` in code-behind, not `@inject` in `.razor`.
- Three rendering states per page: loading / error / content.
- No Bootstrap CSS.
- i18n via JSON files in `wwwroot/locales/{lang}/{namespace}.json`.

## EF migrations

```bash
dotnet ef migrations add <Name> \
  --project Applications/Api \
  --startup-project Applications/Api \
  --output-dir Data/Migrations
```

Migrations in `Applications/Api/Data/Migrations/`. Auto-applied on startup.

## Commit style

`Project - What was done`. Single scope. No body for small changes.
