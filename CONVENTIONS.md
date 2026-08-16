# Conventions Cheat-Sheet

One-page quick reference. Full rationale + the vertical-slice recipe live in [`AGENTS.md`](AGENTS.md). Everything here is **enforced** by `Tests/Architecture.Tests` (NetArchTest + Roslyn) — `dotnet test` is the checker.

## C#

| Rule | Do | Don't |
|---|---|---|
| Namespaces | File-scoped `namespace X.Y;`, namespace = folder path | Block-scoped `{ }` |
| Async naming | `GetUser`, `PostWidget` | `GetUserAsync` |
| Model naming | `GetUser`, `PostUserRequest` | `UserDto` |
| Primary ctors | services, controllers, jobs, mappers, auth handlers | entities |
| `sealed` | services, mappers, jobs, validators, auth handlers | controllers, entities |
| `await` (libraries) | `await x.Foo().ConfigureAwait(false)` | bare `await` in `Shared.*`/`Databases.*` |
| `CancellationToken` | named `ct`, **last** parameter | anywhere else |
| Controllers | throw → `ExceptionMiddleware` maps it | `try/catch` in controllers |
| Async | `async Task` | `async void` |
| Warnings | zero (`TreatWarningsAsErrors=true`) | suppress without reason |

## Method-body spacing

Blank line **between logical stages** — validate, load, apply, persist, map, return — at least one when a body has more than one statement. **No** blank line right after the opening `{` or right before the closing `}`. **Method-like bodies only** (methods, constructors, accessors, lambdas — not types or `if`/`for`/`try`). `dotnet format` won't add these; write them by hand.

```csharp
public GetMe ToGetMe(ApplicationUser user)
{
    var roles = LoadRoles(user);

    return Map(user, roles);
}
```

## Enforced cleanliness (Architecture.Tests + dotnet format)

- No dead public types/constants (`PublicSymbols_All_AreReferenced`)
- No `DateTime.Now` — `DateTime.UtcNow` only
- Logging uses structured placeholders, never interpolated strings
- Controllers: `[ProducesResponseType]` on every action
- Blazor: no `@inject`, no `@code` in `.razor` — code-behind only
- Web: inject `HttpClient` (the one from `Program.cs`); never `new HttpClient()`
- GlobalUsings: framework-only, no `System.*` entries
- Unused usings / using order / naming: enforced by `dotnet format --verify-no-changes`

## Layout (bounded contexts)

| Layer | Path |
|---|---|
| Controller | `Applications/Api/Controllers/{Context}/` |
| Service (`IXService` + sealed `XService`, one file) | `Shared/Services/{Context}/` |
| HTTP models (records) | `Shared/Resources/HTTP/{Context}/{Verb}/` |
| Validator (sealed `AbstractValidator<>`) | `Shared/Resources/Validators/{Context}/` |
| Mapper (`[Mapper]` sealed partial — Mapperly) | `Shared/Mapping/{Context}/` |
| EF config (`IEntityTypeConfiguration`) | `Databases/{Context}/` |
| Entity (not sealed) | `Databases/Core/Entities/` |
| Domain enum (shared) | `Shared/Resources/Enums/` — entity + HTTP records share it; DB stores string (`HasConversion<string>`), JSON string (`JsonStringEnumConverter`) |

## HTTP / EF / Tests

- Responses: `ApiResponse<T>.Ok / .Created / .Fail`. `[ProducesResponseType]` on every action. OpenAPI tags via `OpenApiTagNames` constants.
- EF: singular tables, per-context schemas, enums `HasConversion<string>()`, named constraints `PK-/FK-/IX-Schema_Table_Column`, `.Select()` projections, `.AsSplitQuery()`.
- Tests: xUnit; names `Method_Scenario_Expected`; integration via `WebApplicationFactory` + InMemory; unit via Moq + `NullLogger<T>.Instance`.

## Commits

`Project - What was done` — e.g. `Api - Add Widget endpoints`.
</content>
