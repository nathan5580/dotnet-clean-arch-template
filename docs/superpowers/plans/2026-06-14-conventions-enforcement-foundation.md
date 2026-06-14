# Conventions & Enforcement Foundation — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make the template's coding conventions executable — a dedicated `Tests/Architecture.Tests` project (NetArchTest + Roslyn) that fails CI on violations — swap AutoMapper → Mapperly, apply method-body aeration, and rewrite the convention docs. Blazor stays co-hosted.

**Architecture:** A new xUnit test project references every production assembly. Structural/naming/dependency rules use NetArchTest reflection; body-level rules (try/catch, async-void, ConfigureAwait, aeration, file-scoped namespaces) use Roslyn (`Microsoft.CodeAnalysis.CSharp`) parsing source files located by walking up to the `.slnx`. Existing code is brought into compliance task-by-task so every commit is green; the broad aeration sweep is the final code task so it covers files created earlier.

**Tech Stack:** .NET 10, xUnit, NetArchTest.Rules, Microsoft.CodeAnalysis.CSharp, Riok.Mapperly, EF Core InMemory, Microsoft.NET.Test.Sdk.

**Sub-project A of 4** (see `docs/superpowers/specs/2026-06-14-conventions-enforcement-foundation-design.md`). B = auth completion, C = Blazor/Tailwind polish, D = Products showcase.

---

## File Structure

**Created:**
- `Tests/Architecture.Tests/Architecture.Tests.csproj` — the enforcement project; references all production projects.
- `Tests/Architecture.Tests/GlobalUsings.cs` — shared usings (Xunit, NetArchTest, Roslyn).
- `Tests/Architecture.Tests/SourceFiles.cs` — repo-root discovery + source-file enumeration (excludes obj/bin/Migrations).
- `Tests/Architecture.Tests/StructuralRulesTests.cs` — reflection/NetArchTest rules.
- `Tests/Architecture.Tests/SourceRulesTests.cs` — Roslyn rules.
- `Tests/Architecture.Tests/NamingTests.cs` — test-method naming.

**Modified:**
- `Directory.Packages.props`, `{{ProjectName}}.slnx`, `.editorconfig`
- `Shared/Mapping/Mapping.csproj`, `Shared/Mapping/GlobalUsings.cs`, `Shared/Mapping/Auth/AuthMapper.cs`
- `Applications/Api/Extensions/ServiceExtensions.cs`, `Tests/Shared.Tests/AuthMapperTests.cs`
- `Databases/Core/Entities/Entities.cs`, `Applications/Api/Controllers/Auth/AuthController.cs`
- `Shared/Services/Auth/AuthService.cs`, `Tests/Api.Tests/AuthControllerTests.cs`
- All files with method bodies — aeration sweep (Task 10)
- `AGENTS.md`, `CLAUDE.md`, `.github/copilot-instructions.md`

**Removed:**
- `Tests/Shared.Tests/ConventionTests.cs` (migrated into Architecture.Tests; also currently non-compiling).

---

## Task 1: Fix latent build breaks (entity `Id` hiding + non-compiling ConventionTests)

Two pre-existing breaks stop the solution building:
1. `ApplicationUser`/`ApplicationRole` declare `public Guid Id`, hiding `IdentityUser.Id`/`IdentityRole.Id` (string) — CS0108 under `TreatWarningsAsErrors`.
2. `Tests/Shared.Tests/ConventionTests.cs` references `Api.Controllers.Auth.AuthController`, but `Shared.Tests` does **not** reference `Api` — so `Shared.Tests` cannot compile. Its rules are re-homed into `Architecture.Tests`, so delete it now.

**Files:**
- Modify: `Databases/Core/Entities/Entities.cs`
- Remove: `Tests/Shared.Tests/ConventionTests.cs`

- [ ] **Step 1: Add `new` to both hiding `Id` members**

In `Databases/Core/Entities/Entities.cs`, `ApplicationUser`:

```csharp
    [NotMapped]
    public new Guid Id
    {
        get => Guid.Parse(base.Id);
        set => base.Id = value.ToString();
    }
```

And `ApplicationRole`:

```csharp
    [NotMapped]
    public new Guid Id
    {
        get => Guid.Parse(base.Id);
        set => base.Id = value.ToString();
    }
```

(Leave `UserActionAudit.Id` unchanged — it hides nothing.)

- [ ] **Step 2: Remove the non-compiling ConventionTests**

```bash
git rm Tests/Shared.Tests/ConventionTests.cs
```

- [ ] **Step 3: Build**

Run: `dotnet build "{{ProjectName}}.slnx" -c Release`
Expected: 0 errors, 0 warnings. (If other pre-existing errors appear, fix them minimally before continuing.)

- [ ] **Step 4: Commit**

```bash
git add Databases/Core/Entities/Entities.cs
git commit -m "Template - Fix CS0108 entity Id hiding; remove non-compiling Shared.Tests ConventionTests"
```

---

## Task 2: Swap AutoMapper → Mapperly

**Files:**
- Modify: `Directory.Packages.props`, `Shared/Mapping/Mapping.csproj`, `Shared/Mapping/GlobalUsings.cs`, `Shared/Mapping/Auth/AuthMapper.cs`, `Applications/Api/Extensions/ServiceExtensions.cs`, `Tests/Shared.Tests/AuthMapperTests.cs`, `.editorconfig`

- [ ] **Step 1: Update central packages**

In `Directory.Packages.props`, replace:

```xml
    <!-- Mapping -->
    <PackageVersion Include="AutoMapper" Version="14.0.1" />
```

with:

```xml
    <!-- Mapping -->
    <PackageVersion Include="Riok.Mapperly" Version="4.1.1" />
    <!-- Architecture tests -->
    <PackageVersion Include="NetArchTest.Rules" Version="1.3.2" />
    <PackageVersion Include="Microsoft.CodeAnalysis.CSharp" Version="4.14.0" />
```

(Adding the arch-test versions now so Task 3 doesn't reopen this file.)

- [ ] **Step 2: Point Mapping.csproj at Mapperly**

In `Shared/Mapping/Mapping.csproj`, replace `<PackageReference Include="AutoMapper" />` with `<PackageReference Include="Riok.Mapperly" />`.

- [ ] **Step 3: Update Mapping GlobalUsings**

Replace `Shared/Mapping/GlobalUsings.cs` with:

```csharp
global using Databases.Core.Entities;
global using Riok.Mapperly.Abstractions;
global using Shared.Resources.HTTP.Auth.GET;
```

- [ ] **Step 4: Convert AuthMapper to a Mapperly partial mapper**

Replace `Shared/Mapping/Auth/AuthMapper.cs` with:

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

- [ ] **Step 5: Silence Mapperly's unmapped-source-member diagnostic**

In `.editorconfig`, under `[*.{cs,razor}]` (after `dotnet_diagnostic.IDE0040.severity = warning`), add:

```
# Mapperly: ApplicationUser has many Identity members we intentionally don't map
dotnet_diagnostic.RMG020.severity = none
```

- [ ] **Step 6: Remove AutoMapper registration from ServiceExtensions**

In `Applications/Api/Extensions/ServiceExtensions.cs`, delete this block:

```csharp
        // AutoMapper
        services.AddSingleton<IMapper>(sp =>
            new MapperConfiguration(cfg =>
            {
                cfg.AddMaps(typeof(AuthMappingProfile).Assembly);
            }).CreateMapper());

```

Leave `// Scoped services` (registers `IAuthMapper → AuthMapper`) untouched.

- [ ] **Step 7: Rewrite AuthMapperTests to construct the mapper directly**

Replace `Tests/Shared.Tests/AuthMapperTests.cs` with:

```csharp
using Databases.Core.Entities;
using Shared.Mapping.Auth;

namespace Shared.Tests;

public sealed class AuthMapperTests
{
    [Fact]
    public void ToGetMe_WithValidUser_MapsCorrectly()
    {
        var mapper = new AuthMapper();

        var user = new ApplicationUser
        {
            UserName = "test@example.com",
            Email = "test@example.com",
            CreatedAt = new DateTime(2024, 1, 1),
            IsActive = true
        };

        var result = mapper.ToGetMe(user);

        Assert.Equal(user.Id.ToString(), result.UserId);
        Assert.Equal(user.Email, result.Email);
        Assert.Equal(user.CreatedAt, result.CreatedAt);
        Assert.True(result.IsActive);
    }
}
```

- [ ] **Step 8: Build + test**

Run: `dotnet build "{{ProjectName}}.slnx" -c Release` → 0 errors, 0 warnings.
Run: `dotnet test Tests/Shared.Tests/Shared.Tests.csproj -c Release` → all pass. (If Mapperly emits an RMG error, read it; `[MapProperty]`/`[MapperIgnoreTarget]` cover the `Id→UserId` rename and the ignored `Roles`.)

- [ ] **Step 9: Commit**

```bash
git add Directory.Packages.props Shared/Mapping Applications/Api/Extensions/ServiceExtensions.cs Tests/Shared.Tests/AuthMapperTests.cs .editorconfig
git commit -m "Shared.Mapping - Replace AutoMapper with Mapperly source-generated mappers"
```

---

## Task 3: Scaffold the Architecture.Tests project

**Files:**
- Create: `Tests/Architecture.Tests/Architecture.Tests.csproj`, `GlobalUsings.cs`, `SourceFiles.cs`, `StructuralRulesTests.cs` (smoke test)
- Modify: `{{ProjectName}}.slnx`

- [ ] **Step 1: Create the project file**

Create `Tests/Architecture.Tests/Architecture.Tests.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <RootNamespace>Architecture.Tests</RootNamespace>
    <AssemblyName>Architecture.Tests</AssemblyName>
    <IsPackable>false</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\Applications\Api\Api.csproj" />
    <ProjectReference Include="..\..\Databases\Core\Core.csproj" />
    <ProjectReference Include="..\..\Databases\Auth\Auth.csproj" />
    <ProjectReference Include="..\..\Shared\Services\Services.csproj" />
    <ProjectReference Include="..\..\Shared\Resources\Resources.csproj" />
    <ProjectReference Include="..\..\Shared\Mapping\Mapping.csproj" />
    <ProjectReference Include="..\..\Shared\Jobs\Jobs.csproj" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.EntityFrameworkCore.InMemory" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" />
    <PackageReference Include="NetArchTest.Rules" />
    <PackageReference Include="Microsoft.CodeAnalysis.CSharp" />
    <PackageReference Include="coverlet.collector">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
    <PackageReference Include="xunit" />
    <PackageReference Include="xunit.runner.visualstudio">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
  </ItemGroup>

</Project>
```

- [ ] **Step 2: Create GlobalUsings**

Create `Tests/Architecture.Tests/GlobalUsings.cs`:

```csharp
global using System.Reflection;
global using Microsoft.CodeAnalysis;
global using Microsoft.CodeAnalysis.CSharp;
global using Microsoft.CodeAnalysis.CSharp.Syntax;
global using NetArchTest.Rules;
global using Xunit;
```

- [ ] **Step 3: Create the SourceFiles helper**

Create `Tests/Architecture.Tests/SourceFiles.cs`:

```csharp
namespace Architecture.Tests;

/// <summary>
/// Locates the repository root (directory containing the .slnx) and enumerates
/// first-party C# source files, excluding generated output.
/// </summary>
public static class SourceFiles
{
    private static readonly string[] ExcludedSegments =
    [
        $"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}",
        $"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}",
        $"{Path.DirectorySeparatorChar}Migrations{Path.DirectorySeparatorChar}",
    ];

    public static string RepoRoot { get; } = FindRepoRoot();

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);

        while (dir is not null)
        {
            if (dir.GetFiles("*.slnx").Length > 0)
                return dir.FullName;

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Could not locate repo root (no .slnx found walking up from test bin).");
    }

    public static IReadOnlyList<string> All() =>
        Directory.EnumerateFiles(RepoRoot, "*.cs", SearchOption.AllDirectories)
            .Where(p => !ExcludedSegments.Any(p.Contains))
            .ToList();

    public static IReadOnlyList<string> InProject(string relativeFolder)
    {
        var needle = $"{Path.DirectorySeparatorChar}{relativeFolder}{Path.DirectorySeparatorChar}";
        return All().Where(p => p.Contains(needle)).ToList();
    }

    public static IReadOnlyList<string> EndingWith(string suffix) =>
        All().Where(p => p.EndsWith(suffix, StringComparison.Ordinal)).ToList();

    public static CompilationUnitSyntax Parse(string path) =>
        CSharpSyntaxTree.ParseText(File.ReadAllText(path)).GetCompilationUnitRoot();
}
```

- [ ] **Step 4: Smoke test**

Create `Tests/Architecture.Tests/StructuralRulesTests.cs`:

```csharp
namespace Architecture.Tests;

public sealed class StructuralRulesTests
{
    [Fact]
    public void RepoRoot_Resolves_ToSlnxDirectory()
    {
        Assert.True(Directory.GetFiles(SourceFiles.RepoRoot, "*.slnx").Length > 0);
    }
}
```

- [ ] **Step 5: Add the project to the solution**

In `{{ProjectName}}.slnx`, inside the `/Tests/` folder, add:

```xml
    <Project Path="Tests/Architecture.Tests/Architecture.Tests.csproj" />
```

- [ ] **Step 6: Build + run**

Run: `dotnet test Tests/Architecture.Tests/Architecture.Tests.csproj -c Release` → 1 test, passes.

- [ ] **Step 7: Commit**

```bash
git add Tests/Architecture.Tests "{{ProjectName}}.slnx"
git commit -m "Tests.Architecture - Scaffold architecture test project with source-file helper"
```

---

## Task 4: Un-seal entities and AuthController

**Files:**
- Modify: `Databases/Core/Entities/Entities.cs`, `Applications/Api/Controllers/Auth/AuthController.cs`

- [ ] **Step 1: Remove `sealed` from the three entities**

Change the declarations to `public class ApplicationUser : IdentityUser`, `public class ApplicationRole : IdentityRole`, `public class UserActionAudit` (keep the `new Guid Id` from Task 1).

- [ ] **Step 2: Remove `sealed` from AuthController**

Change `public sealed class AuthController(...)` to `public class AuthController(...)`.

- [ ] **Step 3: Build**

Run: `dotnet build "{{ProjectName}}.slnx" -c Release` → 0 errors, 0 warnings.

- [ ] **Step 4: Commit**

```bash
git add Databases/Core/Entities/Entities.cs Applications/Api/Controllers/Auth/AuthController.cs
git commit -m "Template - Un-seal entities and AuthController per sealed conventions"
```

---

## Task 5: Structural rules (sealed, records, no-Dto, dependency, no-Async-suffix, ct-last, enum→string)

**Files:**
- Modify: `Tests/Architecture.Tests/StructuralRulesTests.cs`

- [ ] **Step 1: Replace StructuralRulesTests with the full rule set**

Replace `Tests/Architecture.Tests/StructuralRulesTests.cs` with:

```csharp
using Databases.Core;
using Microsoft.EntityFrameworkCore;

namespace Architecture.Tests;

public sealed class StructuralRulesTests
{
    private static readonly Assembly ApiAssembly = typeof(Api.Controllers.Auth.AuthController).Assembly;
    private static readonly Assembly ServicesAssembly = typeof(Shared.Services.Auth.AuthService).Assembly;
    private static readonly Assembly MappingAssembly = typeof(Shared.Mapping.Auth.AuthMapper).Assembly;
    private static readonly Assembly ResourcesAssembly = typeof(Shared.Resources.HTTP.Common.ApiResponse).Assembly;
    private static readonly Assembly JobsAssembly = typeof(Shared.Jobs.QuartzExtensions).Assembly;
    private static readonly Assembly CoreAssembly = typeof(Databases.Core.Entities.ApplicationUser).Assembly;

    private static string Describe(TestResult result) =>
        result.IsSuccessful ? "" : "Offending types: " + string.Join(", ", result.FailingTypeNames ?? []);

    private static void AssertSealed(Assembly assembly, string suffix)
    {
        var result = Types.InAssembly(assembly)
            .That().AreClasses().And().AreNotAbstract().And().HaveNameEndingWith(suffix)
            .Should().BeSealed()
            .GetResult();

        Assert.True(result.IsSuccessful, Describe(result));
    }

    [Fact]
    public void Services_Concrete_AreSealed() => AssertSealed(ServicesAssembly, "Service");

    [Fact]
    public void Mappers_Concrete_AreSealed() => AssertSealed(MappingAssembly, "Mapper");

    [Fact]
    public void Jobs_Concrete_AreSealed() => AssertSealed(JobsAssembly, "Job");

    [Fact]
    public void Validators_Concrete_AreSealed() => AssertSealed(ResourcesAssembly, "Validator");

    [Fact]
    public void AuthorizationHandlers_Concrete_AreSealed() => AssertSealed(ApiAssembly, "Handler");

    [Fact]
    public void Controllers_Concrete_AreNotSealed()
    {
        var result = Types.InAssembly(ApiAssembly)
            .That().AreClasses().And().AreNotAbstract().And().HaveNameEndingWith("Controller")
            .Should().NotBeSealed()
            .GetResult();

        Assert.True(result.IsSuccessful, Describe(result));
    }

    [Fact]
    public void Entities_All_AreNotSealed()
    {
        var entities = CoreAssembly.GetTypes()
            .Where(t => t.Namespace?.Contains("Entities") == true && t is { IsClass: true, IsAbstract: false });

        foreach (var type in entities)
            Assert.False(type.IsSealed, $"{type.Name} must not be sealed — EF Core proxies require open entity types.");
    }

    [Fact]
    public void HttpModels_All_AreRecords()
    {
        var httpTypes = ResourcesAssembly.GetTypes()
            .Where(t => t.Namespace?.Contains("HTTP") == true && t is { IsClass: true, IsAbstract: false });

        foreach (var type in httpTypes)
            Assert.True(type.GetMethod("<Clone>$") is not null, $"{type.FullName} must be a record.");
    }

    [Fact]
    public void Types_None_UseDtoSuffix()
    {
        foreach (var assembly in new[] { ApiAssembly, ServicesAssembly, ResourcesAssembly, MappingAssembly })
            foreach (var type in assembly.GetTypes().Where(t => !t.Name.StartsWith('<')))
                Assert.False(type.Name.EndsWith("Dto"), $"{type.FullName} must not use the 'Dto' suffix.");
    }

    [Fact]
    public void Databases_DoNotDependOn_ApiOrServices()
    {
        var result = Types.InAssembly(CoreAssembly)
            .Should().NotHaveDependencyOnAny("Api", "Shared.Services")
            .GetResult();

        Assert.True(result.IsSuccessful, Describe(result));
    }

    [Fact]
    public void SharedLibraries_DoNotDependOn_Api()
    {
        foreach (var assembly in new[] { ServicesAssembly, MappingAssembly, ResourcesAssembly })
        {
            var result = Types.InAssembly(assembly).Should().NotHaveDependencyOn("Api").GetResult();
            Assert.True(result.IsSuccessful, Describe(result));
        }
    }

    [Fact]
    public void ServiceAndControllerMethods_Have_NoAsyncSuffix()
    {
        var types = ServicesAssembly.GetTypes().Where(t => t.Name.EndsWith("Service") && t.IsClass)
            .Concat(ApiAssembly.GetTypes().Where(t => t.Name.EndsWith("Controller") && t.IsClass));

        foreach (var type in types)
            foreach (var method in type.GetMethods().Where(m => m.IsPublic && !m.IsSpecialName && m.DeclaringType == type))
                Assert.False(method.Name.EndsWith("Async"), $"{type.Name}.{method.Name} must not end with 'Async'.");
    }

    [Fact]
    public void AsyncMethods_CancellationToken_IsNamedCtAndLast()
    {
        var types = ServicesAssembly.GetTypes().Where(t => t.IsClass)
            .Concat(ApiAssembly.GetTypes().Where(t => t.Name.EndsWith("Controller")));

        foreach (var type in types)
            foreach (var method in type.GetMethods().Where(m => m.DeclaringType == type))
            {
                var parameters = method.GetParameters();
                if (parameters.All(p => p.ParameterType != typeof(CancellationToken)))
                    continue;

                var last = parameters[^1];
                Assert.True(last.ParameterType == typeof(CancellationToken),
                    $"{type.Name}.{method.Name}: CancellationToken must be the last parameter.");
                Assert.Equal("ct", last.Name);
            }
    }

    [Fact]
    public void EntityEnumProperties_Use_StringConversion()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("arch-tests")
            .Options;

        using var context = new AppDbContext(options);

        foreach (var entity in context.Model.GetEntityTypes())
            foreach (var property in entity.GetProperties())
            {
                var clrType = Nullable.GetUnderlyingType(property.ClrType) ?? property.ClrType;
                if (!clrType.IsEnum)
                    continue;

                var converter = property.GetValueConverter();
                Assert.True(converter is not null && converter.ProviderClrType == typeof(string),
                    $"{entity.ClrType.Name}.{property.Name} is an enum and must use .HasConversion<string>().");
            }
    }
}
```

- [ ] **Step 2: Run the tests**

Run: `dotnet test Tests/Architecture.Tests/Architecture.Tests.csproj -c Release`
Expected: all pass. Entities/controllers were un-sealed in Task 4; `AppDbContext` has no enum properties yet (enum test vacuous); no `*Handler` types exist yet (handler test vacuous).

- [ ] **Step 3: Commit**

```bash
git add Tests/Architecture.Tests/StructuralRulesTests.cs
git commit -m "Tests.Architecture - Add structural rules (sealed, records, no-Dto, layering, ct-last, enum string)"
```

---

## Task 6: Source rules — no try/catch in controllers, no async-void, file-scoped namespaces

**Files:**
- Create: `Tests/Architecture.Tests/SourceRulesTests.cs`

- [ ] **Step 1: Create SourceRulesTests**

Create `Tests/Architecture.Tests/SourceRulesTests.cs`:

```csharp
namespace Architecture.Tests;

public sealed class SourceRulesTests
{
    [Fact]
    public void Controllers_Contain_NoTryCatch()
    {
        foreach (var path in SourceFiles.EndingWith("Controller.cs"))
        {
            var root = SourceFiles.Parse(path);
            var tryCount = root.DescendantNodes().OfType<TryStatementSyntax>().Count();
            Assert.True(tryCount == 0, $"{path} contains {tryCount} try block(s). Controllers must not use try/catch.");
        }
    }

    [Fact]
    public void Solution_Contains_NoAsyncVoid()
    {
        foreach (var path in SourceFiles.All())
        {
            var root = SourceFiles.Parse(path);

            var asyncVoid = root.DescendantNodes().OfType<MethodDeclarationSyntax>()
                .Where(m => m.Modifiers.Any(mod => mod.IsKind(SyntaxKind.AsyncKeyword)))
                .Where(m => m.ReturnType is PredefinedTypeSyntax p && p.Keyword.IsKind(SyntaxKind.VoidKeyword));

            foreach (var method in asyncVoid)
                Assert.Fail($"{path}: '{method.Identifier.Text}' is 'async void'. Use 'async Task'.");
        }
    }

    [Fact]
    public void AllFiles_Use_FileScopedNamespaces()
    {
        foreach (var path in SourceFiles.All())
        {
            var root = SourceFiles.Parse(path);
            var blockScoped = root.DescendantNodes().OfType<NamespaceDeclarationSyntax>().ToList();
            Assert.True(blockScoped.Count == 0, $"{path} uses a block-scoped namespace. Use file-scoped (namespace X;).");
        }
    }
}
```

- [ ] **Step 2: Run**

Run: `dotnet test Tests/Architecture.Tests/Architecture.Tests.csproj -c Release --filter "FullyQualifiedName~SourceRulesTests"`
Expected: all pass (codebase already uses file-scoped namespaces, no try/catch in controllers, no async void). If a file fails, fix it, then re-run.

- [ ] **Step 3: Commit**

```bash
git add Tests/Architecture.Tests/SourceRulesTests.cs
git commit -m "Tests.Architecture - Add Roslyn rules: no try/catch in controllers, no async void, file-scoped namespaces"
```

---

## Task 7: ConfigureAwait rule + bring library awaits into compliance

**Files:**
- Modify: `Tests/Architecture.Tests/SourceRulesTests.cs`, `Shared/Services/Auth/AuthService.cs`

- [ ] **Step 1: Add the ConfigureAwait rule (failing test first)**

In `Tests/Architecture.Tests/SourceRulesTests.cs`, add inside the class:

```csharp
    [Fact]
    public void LibraryAwaits_Use_ConfigureAwaitFalse()
    {
        var libraryFiles = SourceFiles.InProject("Shared").Concat(SourceFiles.InProject("Databases"));

        foreach (var path in libraryFiles)
        {
            var root = SourceFiles.Parse(path);

            foreach (var awaitExpr in root.DescendantNodes().OfType<AwaitExpressionSyntax>())
            {
                var ok = awaitExpr.Expression is InvocationExpressionSyntax inv
                    && inv.Expression is MemberAccessExpressionSyntax member
                    && member.Name.Identifier.Text == "ConfigureAwait";

                var line = awaitExpr.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                Assert.True(ok, $"{path}: await at line {line} must use .ConfigureAwait(false).");
            }
        }
    }
```

- [ ] **Step 2: Run to confirm it fails on AuthService**

Run: `dotnet test Tests/Architecture.Tests/Architecture.Tests.csproj -c Release --filter "FullyQualifiedName~LibraryAwaits"`
Expected: FAIL — `Shared/Services/Auth/AuthService.cs` awaits `FindByEmailAsync`/`CreateAsync` without `ConfigureAwait(false)`.

- [ ] **Step 3: Add ConfigureAwait(false) to AuthService awaits**

In `Shared/Services/Auth/AuthService.cs`, in `Register`:

```csharp
        var existingUser = await userManager.FindByEmailAsync(request.Email).ConfigureAwait(false);
```
```csharp
        var result = await userManager.CreateAsync(user, request.Password).ConfigureAwait(false);
```

- [ ] **Step 4: Run to confirm it passes**

Run: `dotnet test Tests/Architecture.Tests/Architecture.Tests.csproj -c Release --filter "FullyQualifiedName~LibraryAwaits"`
Expected: PASS. (If other library files surface bare awaits, add `.ConfigureAwait(false)` to each — the failure names file + line.)

- [ ] **Step 5: Commit**

```bash
git add Tests/Architecture.Tests/SourceRulesTests.cs Shared/Services/Auth/AuthService.cs
git commit -m "Tests.Architecture - Enforce ConfigureAwait(false) in libraries; fix AuthService awaits"
```

---

## Task 8: Test-naming rule + file-scoped namespace enforcement

**Files:**
- Create: `Tests/Architecture.Tests/NamingTests.cs`
- Modify: `Tests/Architecture.Tests/Architecture.Tests.csproj`, `Tests/Api.Tests/AuthControllerTests.cs`, `.editorconfig`

- [ ] **Step 1: Reference the test projects so naming can reflect over them**

In `Tests/Architecture.Tests/Architecture.Tests.csproj`, add to the project-reference `<ItemGroup>`:

```xml
    <ProjectReference Include="..\Shared.Tests\Shared.Tests.csproj" />
    <ProjectReference Include="..\Api.Tests\Api.Tests.csproj" />
```

(Referenced test assemblies are only reflected over — their `[Fact]`s are not run by the Architecture.Tests runner.)

- [ ] **Step 2: Create the test-naming rule**

Create `Tests/Architecture.Tests/NamingTests.cs`:

```csharp
using System.Text.RegularExpressions;

namespace Architecture.Tests;

public sealed class NamingTests
{
    [Fact]
    public void TestMethods_All_FollowSubjectScenarioExpected()
    {
        var testAssemblies = new[]
        {
            typeof(NamingTests).Assembly,
            typeof(Shared.Tests.AuthMapperTests).Assembly,
            typeof(Api.Tests.AuthControllerTests).Assembly,
        };

        foreach (var assembly in testAssemblies)
        {
            var methods = assembly.GetTypes()
                .SelectMany(t => t.GetMethods())
                .Where(m => m.GetCustomAttributes(typeof(FactAttribute), true).Length > 0
                         || m.GetCustomAttributes(typeof(TheoryAttribute), true).Length > 0);

            foreach (var method in methods)
                Assert.Matches(@"^[A-Z]\w+_[A-Z]\w+_[A-Z]\w+$", method.Name);
        }
    }
}
```

- [ ] **Step 3: Rename the one non-conforming existing test**

`Api.Tests` has `GetHealth_Returns200` (two segments). In `Tests/Api.Tests/AuthControllerTests.cs`, rename to three segments:

```csharp
    public async Task GetHealth_WhenCalled_Returns200()
```

(The others — `Register_WithValidRequest_ReturnsUser`, `Register_WithExistingEmail_ThrowsInvalidOperationException`, `GetAuthMe_WithoutToken_Returns401`, `PostRegister_WithInvalidRequest_Returns400`, `ToGetMe_WithValidUser_MapsCorrectly` — already conform.)

- [ ] **Step 4: Enforce file-scoped namespaces in editorconfig**

In `.editorconfig`, under `[*.{cs,razor}]`, add:

```
csharp_style_namespace_declarations = file_scoped:error
dotnet_diagnostic.IDE0161.severity = error
```

- [ ] **Step 5: Run the suite + build**

Run: `dotnet test Tests/Architecture.Tests/Architecture.Tests.csproj -c Release` → all pass (aeration rule is added next task).
Run: `dotnet build "{{ProjectName}}.slnx" -c Release` → 0 errors, 0 warnings.

- [ ] **Step 6: Commit**

```bash
git add -A
git commit -m "Tests.Architecture - Add test-naming rule; enforce file-scoped namespaces"
```

---

## Task 9: Add the method-body aeration rule (red until the sweep)

**Files:**
- Modify: `Tests/Architecture.Tests/SourceRulesTests.cs`

- [ ] **Step 1: Add the aeration rule**

In `Tests/Architecture.Tests/SourceRulesTests.cs`, add inside the class:

```csharp
    [Fact]
    public void MethodBodies_All_AreAerated()
    {
        var offenders = new List<string>();

        foreach (var path in SourceFiles.All())
        {
            var root = SourceFiles.Parse(path);

            foreach (var method in root.DescendantNodes().OfType<MethodDeclarationSyntax>())
            {
                if (method.Body is not { } body || body.Statements.Count == 0)
                    continue;

                var afterOpen = body.Statements[0].GetLeadingTrivia()
                    .Any(t => t.IsKind(SyntaxKind.EndOfLineTrivia));
                var beforeClose = body.CloseBraceToken.LeadingTrivia
                    .Any(t => t.IsKind(SyntaxKind.EndOfLineTrivia));

                if (!afterOpen || !beforeClose)
                {
                    var line = method.Identifier.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                    offenders.Add($"{path}:{line} {method.Identifier.Text} (afterOpen={afterOpen}, beforeClose={beforeClose})");
                }
            }
        }

        Assert.True(offenders.Count == 0,
            $"These method bodies are not aerated (need a blank line after open brace and before close brace):\n{string.Join("\n", offenders)}");
    }
```

- [ ] **Step 2: Run to confirm it fails and capture the worklist**

Run: `dotnet test Tests/Architecture.Tests/Architecture.Tests.csproj -c Release --filter "FullyQualifiedName~MethodBodies"`
Expected: FAIL listing every non-aerated method (file:line method) across production code, tests, and the Architecture.Tests sources. Keep this output — it is the Task 10 worklist.

- [ ] **Step 3: Commit (test only; intentionally red until Task 10)**

```bash
git add Tests/Architecture.Tests/SourceRulesTests.cs
git commit -m "Tests.Architecture - Add method-body aeration rule (red until aeration sweep)"
```

---

## Task 10: Aeration sweep — aerate every method body

Final code-touching task, so it aerates **all** `.cs` files at once — production code, the `Shared.Tests`/`Api.Tests` methods, and the `Architecture.Tests` sources created in Tasks 3–9.

**Files:**
- Modify: every file listed by the Task 9 failure output.

Transform per offending method: insert one blank line immediately after the opening `{` and one immediately before the closing `}`. Methods only — do **not** aerate type bodies or control-flow blocks.

- [ ] **Step 1: Aerate each offending method**

Example — `Shared/Services/Auth/AuthService.cs` `Register`:

```csharp
    public async Task<(ApplicationUser User, string Token)> Register(PostAuthRegisterRequest request, CancellationToken ct)
    {

        var existingUser = await userManager.FindByEmailAsync(request.Email).ConfigureAwait(false);
        if (existingUser is not null)
            throw new InvalidOperationException("Email is already registered.");

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        var result = await userManager.CreateAsync(user, request.Password).ConfigureAwait(false);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            log.LogError("Registration failed for {Email}: {Errors}", request.Email, errors);
            throw new InvalidOperationException($"Registration failed: {errors}");
        }

        var token = "generate-jwt-token-here"; // Replace with actual JWT generation

        return (user, token);

    }
```

Apply the same transform to every method named in the Task 9 output — including the arch-test methods in `StructuralRulesTests`/`SourceRulesTests`/`NamingTests`/`SourceFiles`, the `ServiceExtensions`/`SeedExtensions`/`ExceptionMiddleware`/`OpenApi`/`Program` methods, every Blazor `.razor.cs` method, and the `Shared.Tests`/`Api.Tests` test methods.

- [ ] **Step 2: Re-run the aeration rule until green**

Run: `dotnet test Tests/Architecture.Tests/Architecture.Tests.csproj -c Release --filter "FullyQualifiedName~MethodBodies"`
Expected: PASS (offender list empty). Iterate using the failure list.

- [ ] **Step 3: Full build + full test**

Run: `dotnet build "{{ProjectName}}.slnx" -c Release` → 0 errors, 0 warnings.
Run: `dotnet test Tests/Architecture.Tests/Architecture.Tests.csproj -c Release` → all pass.

- [ ] **Step 4: Commit**

```bash
git add -A
git commit -m "Template - Aerate all method bodies per the method-body aeration convention"
```

---

## Task 11: Rewrite the convention docs

**Files:**
- Modify: `AGENTS.md`, `CLAUDE.md`, `.github/copilot-instructions.md`

- [ ] **Step 1: Update AGENTS.md**

Keep all Blazor/i18n/CSS sections. Replace the "Mapping Conventions" body with:

```markdown
### Mapping Conventions

- **Mapperly** (source generator) — no runtime mapping dependency
- Wrapper interface per context: `IAuthMapper` → `[Mapper] public sealed partial class AuthMapper`
- Renames/ignores via attributes: `[MapProperty(nameof(Src.X), nameof(Dst.Y))]`, `[MapperIgnoreTarget(nameof(Dst.Z))]`
- Registered as `services.AddScoped<IAuthMapper, AuthMapper>()` (mappers are plain classes — no container)
```

Add after "General C# Conventions":

```markdown
### Method-Body Aeration

Every method with a block body gets one blank line immediately after the opening `{` and one immediately before the closing `}`. Type bodies and control-flow blocks (`if`/`for`/`try`) stay compact.

\```csharp
public GetMe ToGetMe(ApplicationUser user)
{

    var roles = LoadRoles(user);

    return Map(user, roles);

}
\```

Enforced by `Architecture.Tests` (Roslyn). Not auto-fixable by `dotnet format` — write it aerated.
```

Add a top-level section before "Test Conventions":

```markdown
## Architecture Tests

`Tests/Architecture.Tests` makes conventions executable — CI fails on violations:

- **Structural (NetArchTest):** services/mappers/jobs/validators/handlers sealed; controllers + entities not sealed; HTTP models are records; no `Dto` suffix; `ct` named/last; enum properties use `.HasConversion<string>()`; layering (Databases/Shared never depend on Api).
- **Source (Roslyn):** no try/catch in controllers; no `async void`; `ConfigureAwait(false)` on every library await; file-scoped namespaces; method-body aeration.
- **Naming:** test methods match `Subject_Scenario_Expected`.

Add a rule here whenever you add a convention.
```

- [ ] **Step 2: Update CLAUDE.md**

In the stack table, change the Mapping row from `AutoMapper` to `Mapperly`. In the "Hard rules" C# list add:

```markdown
- Method bodies aerated: blank line after opening `{` and before closing `}` (methods only).
- Conventions enforced by `Tests/Architecture.Tests` (NetArchTest + Roslyn) — keep it green.
```

- [ ] **Step 3: Update .github/copilot-instructions.md**

Mirror the three changes (Mapperly instead of AutoMapper, aeration rule, Architecture.Tests note). Keep all Blazor references.

- [ ] **Step 4: Commit**

```bash
git add AGENTS.md CLAUDE.md .github/copilot-instructions.md
git commit -m "Docs - Mapperly, method-body aeration, and Architecture.Tests conventions"
```

---

## Task 12: Final verification

- [ ] **Step 1: Clean build, zero warnings**

Run: `dotnet build "{{ProjectName}}.slnx" -c Release`
Expected: `Build succeeded. 0 Warning(s) 0 Error(s)`.

- [ ] **Step 2: Full test run**

Run: `dotnet test "{{ProjectName}}.slnx" -c Release`
Expected: all projects pass — `Api.Tests`, `Shared.Tests`, `Architecture.Tests`, `Web.Tests`.

- [ ] **Step 3: Confirm AutoMapper is gone from production code**

Run: `grep -rn "AutoMapper" Applications Shared Databases --include=*.cs --include=*.csproj`
Expected: no matches.

- [ ] **Step 4: Confirm formatting is stable**

Run: `dotnet format "{{ProjectName}}.slnx" --verify-no-changes`
Expected: no changes required. (If it reports diffs unrelated to aeration, apply them and re-commit.)

- [ ] **Step 5: Final commit if anything changed**

```bash
git add -A
git commit -m "Template - Sub-project A verification: build clean, all tests green" || echo "nothing to commit"
```
</content>
