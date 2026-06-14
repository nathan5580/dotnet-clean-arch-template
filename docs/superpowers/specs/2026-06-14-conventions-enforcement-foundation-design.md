# Sub-project A — Conventions & Enforcement Foundation

**Date:** 2026-06-14
**Status:** Draft (awaiting review)
**Supersedes:** the Blazor-removal premise of `2026-06-08-template-polish-design.md` (Blazor is being **kept**, co-hosted). The non-Blazor correctness fixes from that spec survive in Sub-project B.

## Context: the roadmap

This template is a clean .NET 10 + co-hosted Blazor WASM + Tailwind reference, with conventions distilled from the **Recreo** and **n8Booking** apps (`/Users/home/RiderProjects/{Recreo,n8Booking}`). The full polish effort is decomposed into four sequenced sub-projects, each with its own spec → plan → implementation cycle:

- **A — Conventions & Enforcement Foundation** ← *this spec*
- **B — Auth vertical correctness & completion** (un-seal already folded into A; B adds `IJwtService`, `Login`, `GetMe` roles, role seeding, `ExceptionMiddleware` shape, functional tests)
- **C — Blazor co-hosting & Tailwind polish**
- **D — Products showcase (end-to-end vertical)**

Everything in B/C/D must pass the guardrails this sub-project establishes.

## Goal

Lock the canonical convention set, make it **executable** (a dedicated `Architecture.Tests` project that fails CI on violations), document it authoritatively, and bring the existing code into compliance — including swapping AutoMapper for **Mapperly** and applying the **method-body aeration** house rule.

## Out of scope (deferred to later sub-projects)

- JWT generation, `Login` implementation, role seeding, `GetMe` roles, `ExceptionMiddleware` body shape → **B**
- Blazor UI / Tailwind theme / i18n work → **C**
- Products bounded context → **D**

---

## 1. Decisions (locked)

| Topic | Decision |
|---|---|
| Frontend | **Keep** Blazor WASM co-hosted by the API + Tailwind v4 |
| Mapping | **Mapperly** (`Riok.Mapperly`, source-gen) — remove AutoMapper entirely |
| Aeration | Blank line after a method's opening `{` and before its closing `}`. **Method bodies only** — type bodies and control-flow blocks stay compact |
| Arch enforcement | Dedicated `Tests/Architecture.Tests` — **NetArchTest** for structural/naming/dependency rules + **Roslyn** (`Microsoft.CodeAnalysis.CSharp`) for body-level/source rules |
| i18n | English only (machinery present) |
| Convention base | Existing `AGENTS.md` + patterns extracted from Recreo & n8Booking |

---

## 2. The convention set the tests enforce

### Structural / naming / dependency (NetArchTest, reflection)

1. Concrete `*Service` types are `sealed`.
2. Concrete `*Mapper` types are `sealed`.
3. Concrete `*Job` types are `sealed`.
4. Concrete authorization handler types (`*Handler`) are `sealed`.
5. Concrete `*Controller` types are **not** `sealed`.
6. Types in `Databases.Core.Entities` are **not** `sealed`.
7. Concrete `*Validator` types are `sealed` and derive from `AbstractValidator<>`.
8. HTTP model types (namespace contains `HTTP`, concrete) are **records**.
9. No type name ends with `Dto`.
10. Dependency direction: `Databases.*` must not depend on `Shared.Services` or `Api`; `Shared.*` must not depend on `Api`. (Layering integrity.)

### Body-level / source rules (Roslyn syntax analysis)

11. No `try`/`catch` inside `*Controller.cs`.
12. No method name (services + controllers) ends with `Async`.
13. No `async void` anywhere in the solution.
14. Every `await` in library projects (`Shared.*`, `Databases.*`) is followed by `.ConfigureAwait(false)`.
15. Async methods that take a `CancellationToken` name it `ct` and place it **last**.
16. Enum properties in EF entity-configuration files use `.HasConversion<string>()`. *(Passes vacuously until D adds enum-bearing configs.)*
17. Every file uses a **file-scoped** namespace (no block-scoped). *(Also enforced as an `.editorconfig` build error; the test is the backstop.)*
18. **Aeration:** every method with a block body containing ≥1 statement has a blank line immediately after `{` and immediately before `}`.

### Naming (test suite self-discipline)

19. All `[Fact]`/`[Theory]` method names match `^[A-Z]\w+_[A-Z]\w+_[A-Z]\w+$` (Subject_Scenario_Expected, digits allowed — fixes the current regex that rejects `_Returns200`).

> Rule 16 ships now and passes vacuously; the guardrail is in place before the feature that needs it (D) arrives.
>
> **Deferred to B:** the "feature controllers inherit `AuthenticatedController` (allow-list: `AuthController`)" rule is added in Sub-project B, alongside the `AuthenticatedController` type itself — a test cannot reference a type that doesn't yet exist.

---

## 3. `Architecture.Tests` project

New project `Tests/Architecture.Tests/Architecture.Tests.csproj`:

- References **all** production projects (`Api`, `Databases.*`, `Shared.*`) so it can reflect over every assembly and resolve source paths.
- Packages: `xunit`, `xunit.runner.visualstudio`, `Microsoft.NET.Test.Sdk`, `coverlet.collector`, **`NetArchTest.Rules`**, **`Microsoft.CodeAnalysis.CSharp`**.
- Layout:
  - `StructuralRulesTests.cs` — rules 1–10 via NetArchTest.
  - `SourceRulesTests.cs` — rules 11–18 via Roslyn (a small helper enumerates `*.cs` under the repo root, parses each into a `SyntaxTree`, and asserts).
  - `NamingTests.cs` — rule 19 (test-method naming).
  - `SourceFiles.cs` — helper to locate the repo root from the test bin dir and enumerate source files by project/glob (parametrized, not hard-coded depth).
- The existing `Tests/Shared.Tests/ConventionTests.cs` is **removed** (its rules migrate here; `Shared.Tests` keeps only true unit tests). This also resolves the current layering smell where `Shared.Tests` reaches into `Api.Controllers`.

A Roslyn aeration check (rule 18) sketch:

```
foreach method with BlockSyntax body and ≥1 statement:
    assert blank line between '{' and first statement
        (first statement's leading trivia contains ≥2 EndOfLineTrivia)
    assert blank line between last statement and '}'
        (closing brace leading trivia contains ≥2 EndOfLineTrivia)
```

---

## 4. Mapperly migration

Replace AutoMapper across the solution:

- `Directory.Packages.props`: remove `AutoMapper`; add `Riok.Mapperly`. (AutoMapper Blazor packages are unrelated and stay.)
- `Shared/Mapping/Mapping.csproj`: reference `Riok.Mapperly`.
- `Shared/Mapping/GlobalUsings.cs`: drop `global using AutoMapper;`, add `global using Riok.Mapperly.Abstractions;`.
- `Shared/Mapping/Auth/AuthMapper.cs`: convert to a Mapperly partial mapper, delete `AuthMappingProfile`:

```csharp
namespace Shared.Mapping.Auth;

public interface IAuthMapper
{
    GetMe ToGetMe(ApplicationUser user);
}

[Mapper]
public sealed partial class AuthMapper : IAuthMapper
{
    [MapProperty(nameof(ApplicationUser.Id), nameof(GetMe.UserId))]
    [MapperIgnoreTarget(nameof(GetMe.Roles))]
    public partial GetMe ToGetMe(ApplicationUser user);
}
```

- `Applications/Api/Extensions/ServiceExtensions.cs`: delete the `AddSingleton<IMapper>(... MapperConfiguration ...)` block. `IAuthMapper → AuthMapper` stays registered scoped. Mapperly mappers are plain classes — no runtime container.
- Update `AuthMapperTests` to construct `new AuthMapper()` directly (no `IMapper`).

---

## 5. `.editorconfig` additions

Add to the `[*.{cs}]` section:

```
csharp_style_namespace_declarations = file_scoped:error
dotnet_diagnostic.IDE0161.severity = error   # block-scoped namespace -> error
```

Keep everything else. Aeration is **not** added here (no `dotnet format` rule exists for it) — it is enforced by the Roslyn test (rule 18) and documented as a house rule. CI gains a `dotnet format --verify-no-changes` step in a later CI pass.

---

## 6. Bring existing code into compliance

- **Un-seal** `ApplicationUser`, `ApplicationRole`, `UserActionAudit` (`Databases/Core/Entities/Entities.cs`) and `AuthController` — required by rules 5–6.
- Fix any pre-existing build break surfaced while un-sealing (the `Id` members hide `IdentityUser.Id`; add `new` so the build is warning-clean under `TreatWarningsAsErrors`).
- **Aerate** all existing method bodies across `Api`, `Shared.*`, `Databases.*`, `Tests.*`, and Blazor `.razor.cs` files.
- Ensure all files are file-scoped-namespace clean.

---

## 7. Documentation rewrite

- **`AGENTS.md`** — keep the Blazor/i18n/CSS sections (Blazor stays); replace AutoMapper guidance with **Mapperly**; add the **aeration** rule with a before/after example; add an **Architecture.Tests** section listing the enforced rules; tighten the `sealed` table to match the executable rules.
- **`CLAUDE.md`** — change the Mapping row to Mapperly; add aeration + arch-tests to the hard rules; keep Blazor rows and run commands.
- **`.github/copilot-instructions.md`** — mirror the above (Mapperly, aeration, keep Blazor).
- Add a one-line **SUPERSEDED** banner to `2026-06-08-template-polish-design.md` and its plan, pointing here.

---

## 8. Exit criteria

1. `dotnet build {{ProjectName}}.slnx -c Release` → **0 warnings, 0 errors**.
2. `dotnet test` → **all green**, including the new `Architecture.Tests`.
3. `Architecture.Tests` contains rules 1–20; rules that depend on later features (16, 19) pass vacuously.
4. No `AutoMapper` reference remains in any non-Blazor production project; `AuthMapper` is a Mapperly partial.
5. Existing method bodies are aerated; `.editorconfig` enforces file-scoped namespaces.
6. `AGENTS.md`, `CLAUDE.md`, `.github/copilot-instructions.md` reflect: Blazor kept, Mapperly, aeration, Architecture.Tests.

---

## 9. Files

**Created:** `Tests/Architecture.Tests/Architecture.Tests.csproj`, `StructuralRulesTests.cs`, `SourceRulesTests.cs`, `NamingTests.cs`, `SourceFiles.cs`, `GlobalUsings.cs`.

**Modified:** `Directory.Packages.props`, `{{ProjectName}}.slnx`, `.editorconfig`, `Shared/Mapping/Mapping.csproj`, `Shared/Mapping/GlobalUsings.cs`, `Shared/Mapping/Auth/AuthMapper.cs`, `Applications/Api/Extensions/ServiceExtensions.cs`, `Databases/Core/Entities/Entities.cs`, `Applications/Api/Controllers/Auth/AuthController.cs`, `Tests/Shared.Tests/AuthMapperTests.cs`, `AGENTS.md`, `CLAUDE.md`, `.github/copilot-instructions.md`, the two `2026-06-08` docs (superseded banner), + every file touched by aeration.

**Removed:** `Tests/Shared.Tests/ConventionTests.cs` (migrated to `Architecture.Tests`).
</content>
</invoke>
