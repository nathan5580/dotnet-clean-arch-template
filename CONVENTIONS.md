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

## Method-body aeration

Blank line right after a method's opening `{` and right before its closing `}` — **methods only** (not types or `if`/`for`/`try`). `dotnet format` won't add these; write them by hand.

```csharp
public GetMe ToGetMe(ApplicationUser user)
{

    return Map(user);

}
```

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

## HTTP / EF / Tests

- Responses: `ApiResponse<T>.Ok / .Created / .Fail`. `[ProducesResponseType]` on every action. OpenAPI tags via `OpenApiTagNames` constants.
- EF: singular tables, per-context schemas, enums `HasConversion<string>()`, named constraints `PK-/FK-/IX-Schema_Table_Column`, `.Select()` projections, `.AsSplitQuery()`.
- Tests: xUnit; names `Method_Scenario_Expected`; integration via `WebApplicationFactory` + InMemory; unit via Moq + `NullLogger<T>.Instance`.

## Commits

`Project - What was done` — e.g. `Api - Add Widget endpoints`.
</content>
