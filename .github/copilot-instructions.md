You are working on {{ProjectName}}, a .NET 10 application with an ASP.NET Core API and a Blazor WebAssembly frontend.

Read AGENTS.md at the repo root before making non-trivial changes (start with its "For AI Agents — Start Here" section). CONVENTIONS.md is a one-page cheat-sheet.

Repository structure
- Applications/Api: main host application, controllers, middleware, OpenAPI/Scalar, auth
- Applications/Web: Blazor WebAssembly frontend, Tailwind CSS
- Shared/Resources: HTTP models, validators, enums
- Shared/Services: business logic services by bounded context
- Shared/Mapping: Mapperly source-generated mapper classes (one per bounded context)
- Shared/Jobs: Quartz jobs
- Databases/Core: EF Core entities, DbContext, configurations
- Tests/: xUnit test projects (Api.Tests, Architecture.Tests, Shared.Tests, Web.Tests)

Project conventions
- No top-level statements
- Use primary constructors for services, controllers, jobs
- Keep interface + implementation in the same file, named after the implementation
- Do not add an Async suffix to method names
- Do not use Dto suffix anywhere
- Follow bounded contexts consistently — organize code by business domain across all layers
- Controllers and services use verb-first names: GetUser, PostCompany, DeleteBooking
- HTTP models: Get{Resource} (read), {Verb}{Resource}Request (write)
- HTTP models are record types, never class
- Keep code simple and readable; avoid unnecessary abstractions
- sealed services/jobs/mappers; never sealed controllers or entities
- Mapping via Mapperly (source generator): `[Mapper] public sealed partial class AuthMapper`, registered as `services.AddScoped<IAuthMapper, AuthMapper>()`
- Method bodies aerated: one blank line after the opening `{` and one before the closing `}` (methods only; type bodies and control-flow blocks stay compact)
- Conventions enforced by Tests/Architecture.Tests (NetArchTest + Roslyn) — CI fails on violations; keep it green

Validation commands
- dotnet restore {{ProjectName}}.slnx
- dotnet build {{ProjectName}}.slnx --configuration Release
- dotnet test Tests/Api.Tests/Api.Tests.csproj --configuration Release
- dotnet test Tests/Architecture.Tests/Architecture.Tests.csproj --configuration Release
- dotnet test Tests/Shared.Tests/Shared.Tests.csproj --configuration Release
- dotnet test Tests/Web.Tests/Web.Tests.csproj --configuration Release

Git conventions
- Commit format: Project - What has been done
- Example: Api - Added user registration endpoint
