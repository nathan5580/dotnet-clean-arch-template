# Method-Body Aeration Rule Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a Roslyn architecture test rule that enforces blank lines after `{` and before `}` in all method block bodies, then aerate every offending method in the solution.

**Architecture:** The rule lives in `Tests/Architecture.Tests/SourceRulesTests.cs` as a new `[Fact]` that walks the Roslyn AST of every source file, checks `MethodDeclarationSyntax` block bodies for the leading trivia pattern, and reports violations. Production and test source files are then edited to add the blank lines where missing.

**Tech Stack:** Roslyn (Microsoft.CodeAnalysis.CSharp), xUnit, .NET 10 C#

---

### Task 1: Add the MethodBodies_All_AreAerated test

**Files:**
- Modify: `Tests/Architecture.Tests/SourceRulesTests.cs`

- [ ] **Step 1: Add the test method** inside `SourceRulesTests` class (after the last `}`-before-class-close):

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

- [ ] **Step 2: Verify the test exists and build compiles**

Run: `dotnet build Tests/Architecture.Tests/Architecture.Tests.csproj -c Release`
Expected: Build succeeds, 0 errors

---

### Task 2: Capture the offender worklist

**Files:** None created; read test output only.

- [ ] **Step 1: Run the new test to get the failing worklist**

Run:
```bash
dotnet test Tests/Architecture.Tests/Architecture.Tests.csproj -c Release --filter "FullyQualifiedName~MethodBodies" 2>&1 | tail -60
```

Expected: FAIL. The output will list every `path:line methodName (afterOpen=X, beforeClose=Y)`. This is your authoritative worklist.

- [ ] **Step 2: Group by file**

From the failure message, note which files need edits. Each entry says which side is missing: `afterOpen=False` means add blank line after `{`; `beforeClose=False` means add blank line before `}`.

---

### Task 3: Aerate all offending methods

**Files:** Every `.cs` file listed in the worklist (production + test files).

For each file, for each flagged method:

- [ ] **Step 1: Locate the method**

Use `path:line` from the worklist. Open the file, go to line N (the method signature line).

- [ ] **Step 2: Insert blank line after opening `{`**

If `afterOpen=False`: After the line containing `{` (the line after the method signature), insert one blank line so the first statement is preceded by an empty line.

Before:
```csharp
public Foo DoThing(Bar bar)
{
    var x = bar.Value;
    return x;
}
```

After:
```csharp
public Foo DoThing(Bar bar)
{

    var x = bar.Value;
    return x;

}
```

- [ ] **Step 3: Insert blank line before closing `}`**

If `beforeClose=False`: Before the closing `}` of the method, insert one blank line.

- [ ] **Step 4: Do NOT touch:**
  - Type bodies (class/record/interface braces)
  - Control-flow blocks (`if`, `for`, `foreach`, `while`, `try`, `catch`, `switch`)
  - Property accessors (`get { }`, `set { }`)
  - Constructors (these are `ConstructorDeclarationSyntax`, not `MethodDeclarationSyntax` — the rule skips them automatically)
  - Expression-bodied members (`=> ...`)
  - Methods with no statements (empty bodies — the rule skips these too)

---

### Task 4: Iterate to green

- [ ] **Step 1: Re-run the aeration test**

```bash
dotnet test Tests/Architecture.Tests/Architecture.Tests.csproj -c Release --filter "FullyQualifiedName~MethodBodies"
```

Expected: PASS (0 offenders). If still failing, the output lists the remaining offenders — fix them and re-run.

---

### Task 5: Full solution verification

- [ ] **Step 1: Build the entire solution**

```bash
dotnet build "{{ProjectName}}.slnx" -c Release
```

Expected: 0 errors, 0 warnings.

- [ ] **Step 2: Run Architecture.Tests (all 20 tests)**

```bash
dotnet test Tests/Architecture.Tests/Architecture.Tests.csproj -c Release
```

Expected: 20 passed, 0 failed.

- [ ] **Step 3: Run full solution tests**

```bash
dotnet test "{{ProjectName}}.slnx" -c Release
```

Expected: All test projects pass (Api.Tests, Shared.Tests, Architecture.Tests, Web.Tests).

---

### Task 6: Commit

- [ ] **Step 1: Stage and commit**

```bash
git add -A
git commit -m "Template - Add method-body aeration rule and aerate all method bodies"
```

Expected: Commit succeeds on branch `feature/conventions-enforcement-foundation`.
