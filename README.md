# {{ProjectName}}

{{ProjectDescription}}

Built with **.NET 10** + **Blazor WebAssembly** + **SQL Server** / **PostgreSQL**.

## Architecture

This template follows a **clean/onion architecture with bounded contexts** (modular monolith).

```
Applications/Api/       → ASP.NET Core 10 Web API (co-hosts Blazor WASM)
Applications/Web/       → Blazor WebAssembly 10 client (Tailwind CSS v4)
Databases/              → EF Core configuration per bounded context
Shared/                 → Services, HTTP models, validators, mapping, jobs
Tests/                  → xUnit integration + unit + convention tests
```

**Key architectural decisions:**

- **Co-hosted Blazor WASM** — the API serves both REST endpoints and the Blazor frontend
- **Bounded contexts** — code organized by business domain across all layers (Controllers, Services, HTTP models, EF Core config)
- **Central package management** — all NuGet versions in `Directory.Packages.props`
- **Explicit conventions** — enforced by convention tests in CI, not just docs
- **No DTO suffix, no Async suffix** — clean, verb-first naming throughout
- **`.slnx` format** — modern XML-based solution files

## Quick Start

```bash
# 1. Start dev database
docker compose -f docker-compose.devdb.yml up -d

# 2. Build
dotnet build

# 3. Run API (hosts frontend at http://localhost:5050)
dotnet run --project Applications/Api

# 4. (Optional) Run Web standalone for UI dev at http://localhost:5129
dotnet run --project Applications/Web

# 5. Run tests
dotnet test
```

API docs: `http://localhost:5050/docs/v1` (Scalar UI)

## Project Conventions

Full details in [`AGENTS.md`](AGENTS.md). Quick summary:

| Convention | Rule |
|------------|------|
| No top-level statements | Explicit `Main` method always |
| File-scoped namespaces | `namespace X.Y.Z;` |
| Primary constructors | On services, controllers, jobs, mappers |
| No `Async` suffix | `GetUser`, not `GetUserAsync` |
| No `Dto` suffix | `GetUser`, `PostUserRequest` |
| `sealed` | Services, jobs, mappers (not controllers/entities) |
| `record` types | All HTTP request/response models |
| `.ConfigureAwait(false)` | Every await in library code |
| `CancellationToken ct` | Last parameter, propagated everywhere |
| No try/catch in controllers | `ExceptionMiddleware` handles |
| Test naming | `MethodName_Scenario_ExpectedResult` |

## Agent Support

This template includes instructions for AI coding assistants:

- [`AGENTS.md`](AGENTS.md) — full architecture guide (read by all agents)
- [`CLAUDE.md`](CLAUDE.md) — compact Claude-specific instructions
- [`.github/copilot-instructions.md`](.github/copilot-instructions.md) — GitHub Copilot instructions

## Adding a Bounded Context

1. Create entity in `Databases/Core/Entities/`
2. Create EF config in `Databases/{Context}/`
3. Create HTTP models in `Shared/Resources/HTTP/{Context}/`
4. Create service in `Shared/Services/{Context}/`
5. Create controller in `Applications/Api/Controllers/{Context}/`
6. Create Blazor pages in `Applications/Web/Pages/{Context}/`
7. Add tests in `Tests/`

## CI/CD

- Build + test on push/PR to `main`/`dev`
- Code coverage via coverlet + Codecov
- Dependabot for NuGet + npm + GitHub Actions
- Conventional commit PR titles enforced

## License

MIT
