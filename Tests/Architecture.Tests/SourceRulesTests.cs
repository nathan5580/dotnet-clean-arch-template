using System.Text.RegularExpressions;

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

    [Fact]
    public void MethodLikeBodies_Separate_LogicalStages()
    {
        var offenders = new List<string>();

        foreach (var path in SourceFiles.All())
        {
            var root = SourceFiles.Parse(path);

            foreach (var body in MethodLikeBodies(root).Where(b => b.Statements.Count > 0))
            {
                var afterOpen = HasBoundaryBlankLine(body.Statements[0].GetLeadingTrivia());
                var beforeClose = HasBoundaryBlankLine(body.CloseBraceToken.LeadingTrivia);
                var hasStageBreak = body.Statements.Count > 1 && HasStageBreak(body);

                if (afterOpen || beforeClose || (body.Statements.Count > 1 && !hasStageBreak))
                {
                    var line = body.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                    offenders.Add($"{path}:{line} (afterOpen={afterOpen}, beforeClose={beforeClose}, stageBreak={hasStageBreak})");
                }
            }
        }

        Assert.True(offenders.Count == 0,
            $"These bodies carry a blank line right inside a brace or fail to separate logical stages with a blank line:\n{string.Join("\n", offenders)}");
    }

    private static bool HasStageBreak(BlockSyntax body)
    {
        for (var i = 1; i < body.Statements.Count; i++)
        {
            if (HasBoundaryBlankLine(body.Statements[i].GetLeadingTrivia()))
                return true;
        }

        return false;
    }

    [Fact]
    public void Controllers_Actions_HaveProducesResponseType()
    {
        var offenders = new List<string>();

        foreach (var path in SourceFiles.EndingWith("Controller.cs"))
        {
            var root = SourceFiles.Parse(path);

            foreach (var method in root.DescendantNodes().OfType<MethodDeclarationSyntax>())
            {
                var attributes = method.AttributeLists.SelectMany(a => a.Attributes).ToList();
                var hasHttpAttribute = attributes.Any(a => a.Name.ToString().StartsWith("Http", StringComparison.Ordinal));

                if (!hasHttpAttribute)
                    continue;

                if (!attributes.Any(a => a.Name.ToString().Contains("ProducesResponseType")))
                {
                    var line = method.Identifier.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                    offenders.Add($"{path}:{line} {method.Identifier.Text}");
                }
            }
        }

        Assert.True(offenders.Count == 0,
            $"These controller actions are missing [ProducesResponseType]:\n{string.Join("\n", offenders)}");
    }

    [Fact]
    public void RazorFiles_Contain_NoInject()
    {
        foreach (var path in SourceFiles.RazorFiles())
        {
            var lines = File.ReadAllLines(path);
            var lineNumber = Array.FindIndex(lines, l => l.Contains("@inject")) + 1;
            Assert.True(lineNumber == 0, $"{path} uses '@inject' at line {lineNumber}. Inject via [Inject] in the code-behind instead.");
        }
    }

    [Fact]
    public void RazorPages_All_HaveCodeBehind()
    {
        var pagesDir = Path.Combine(SourceFiles.RepoRoot, "Applications", "Web", "Pages");

        foreach (var page in Directory.EnumerateFiles(pagesDir, "*.razor", SearchOption.AllDirectories))
        {
            var codeBehind = page + ".cs";
            Assert.True(File.Exists(codeBehind),
                $"{page} has no code-behind ({Path.GetFileName(codeBehind)}). Pages live in a .razor.cs partial class.");
        }
    }

    [Fact]
    public void WebCode_Contain_NoNewHttpClient()
    {
        foreach (var path in SourceFiles.InProject("Web"))
        {
            var root = SourceFiles.Parse(path);

            foreach (var creation in root.DescendantNodes()
                .OfType<ObjectCreationExpressionSyntax>()
                .Where(o => o.Type.ToString() == "HttpClient" && o.ArgumentList is not null))
            {
                var line = creation.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                Assert.Fail($"{path}:{line} creates 'new HttpClient(...)'. Inject the HttpClient registered in Program.cs instead.");
            }
        }
    }

    [Fact]
    public void PublicSymbols_All_AreReferenced()
    {
        var files = SourceFiles.All();
        var texts = files.ToDictionary(f => f, File.ReadAllText);
        var offenders = new List<string>();

        foreach (var path in files)
        {
            // Test classes are discovered by the xUnit runner via reflection, not by name.
            if (path.Contains($"{Path.DirectorySeparatorChar}Tests{Path.DirectorySeparatorChar}"))
                continue;

            var root = SourceFiles.Parse(path);

            foreach (var symbol in DeclaredPublicSymbols(root))
            {
                if (IsExempt(symbol))
                    continue;

                var referenced = texts
                    .Where(kv => kv.Key != path)
                    .Any(kv => Regex.IsMatch(kv.Value, $@"{Regex.Escape(symbol.Name)}"));

                if (!referenced)
                    offenders.Add($"{path}: {symbol.Name}");
            }
        }

        Assert.True(offenders.Count == 0,
            $"These public symbols are never referenced outside their declaration file (dead scaffolding):\n{string.Join("\n", offenders)}");
    }

    [Fact]
    public void Solution_Contain_NoDateTimeNow()
    {
        foreach (var path in SourceFiles.All())
        {
            var root = SourceFiles.Parse(path);

            var offenders = root.DescendantNodes()
                .OfType<MemberAccessExpressionSyntax>()
                .Where(m => m.Name.Identifier.Text == "Now"
                    && m.Expression is IdentifierNameSyntax { Identifier.Text: "DateTime" });

            foreach (var offender in offenders)
            {
                var line = offender.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                Assert.Fail($"{path}:{line} uses DateTime.Now. Use DateTime.UtcNow.");
            }
        }
    }

    [Fact]
    public void RazorFiles_Contain_NoCodeBlock()
    {
        foreach (var path in SourceFiles.RazorFiles())
        {
            var text = File.ReadAllText(path);
            Assert.False(text.Contains("@code"), $"{path} contains an @code block. Put logic in a .razor.cs partial class.");
        }
    }

    [Fact]
    public void LogCalls_All_UseStructuredPlaceholders()
    {
        var offenders = new List<string>();

        foreach (var path in SourceFiles.All())
        {
            var root = SourceFiles.Parse(path);

            foreach (var invocation in root.DescendantNodes().OfType<InvocationExpressionSyntax>())
            {
                if (invocation.Expression is not MemberAccessExpressionSyntax member)
                    continue;

                if (!_logMethodNames.Contains(member.Name.Identifier.Text))
                    continue;

                if (invocation.ArgumentList.Arguments.FirstOrDefault()?.Expression is InterpolatedStringExpressionSyntax)
                {
                    var line = invocation.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                    offenders.Add($"{path}:{line} {member.Name.Identifier.Text} uses string interpolation. Use structured placeholders (\"... {{Key}}\", value).");
                }
            }
        }

        Assert.True(offenders.Count == 0,
            $"These log calls interpolate strings (structured placeholders keep logs greppable and safe):\n{string.Join("\n", offenders)}");
    }

    [Fact]
    public void GlobalUsings_Contain_NoSystemUsings()
    {
        foreach (var path in SourceFiles.All().Where(p => p.EndsWith("GlobalUsings.cs", StringComparison.Ordinal)))
        {
            var lines = File.ReadAllLines(path);
            var lineNumber = Array.FindIndex(lines, l => l.StartsWith("global using System.", StringComparison.Ordinal)) + 1;
            Assert.True(lineNumber == 0,
                $"{path}:{lineNumber} declares 'global using System.*'. Put one-off System usings in the file that uses them (implicit usings cover the rest).");
        }
    }

    private static readonly HashSet<string> _logMethodNames =
        new() { "LogInformation", "LogError", "LogWarning", "LogDebug", "LogTrace", "LogCritical" };

    private static IEnumerable<BlockSyntax> MethodLikeBodies(CompilationUnitSyntax root) =>
        root.DescendantNodes()
            .SelectMany(node => (IEnumerable<BlockSyntax>)(node switch
            {
                MethodDeclarationSyntax m => m.Body is { } mb ? [mb] : [],
                ConstructorDeclarationSyntax c => c.Body is { } cb ? [cb] : [],
                DestructorDeclarationSyntax d => d.Body is { } db ? [db] : [],
                OperatorDeclarationSyntax o => o.Body is { } ob ? [ob] : [],
                ConversionOperatorDeclarationSyntax c => c.Body is { } cb2 ? [cb2] : [],
                LocalFunctionStatementSyntax l => l.Body is { } lb ? [lb] : [],
                AccessorDeclarationSyntax a => a.Body is { } ab ? [ab] : [],
                AnonymousFunctionExpressionSyntax f => f.Body is BlockSyntax fb ? [fb] : [],
                _ => [],
            }));

    private static bool HasBoundaryBlankLine(SyntaxTriviaList trivia)
    {
        return trivia.Count > 0 && trivia[0].IsKind(SyntaxKind.EndOfLineTrivia);
    }

    private static IEnumerable<(string Name, bool IsType)> DeclaredPublicSymbols(CompilationUnitSyntax root)
    {
        foreach (var type in root.DescendantNodes().OfType<BaseTypeDeclarationSyntax>())
        {
            if (type.Parent is not CompilationUnitSyntax || !type.Modifiers.Any(m => m.IsKind(SyntaxKind.PublicKeyword)))
                continue;

            yield return (type.Identifier.Text, true);

            if (type is ClassDeclarationSyntax classDecl)
            {
                foreach (var field in classDecl.Members.OfType<FieldDeclarationSyntax>())
                {
                    var isPublicConst = field.Modifiers.Any(m => m.IsKind(SyntaxKind.PublicKeyword))
                        && field.Modifiers.Any(m => m.IsKind(SyntaxKind.ConstKeyword));

                    if (!isPublicConst)
                        continue;

                    foreach (var variable in field.Declaration.Variables)
                        yield return (variable.Identifier.Text, false);
                }
            }
        }
    }

    private static bool IsExempt((string Name, bool IsType) symbol)
    {
        var (name, isType) = symbol;

        if (!isType)
            return false;

        // Entry points invoked by the host, not by name.
        if (name == "Program")
            return true;

        // Extension-method containers (used via `using`), auto-discovered validators, and
        // middleware registered through an extension method in the same file.
        if (name.EndsWith("Extensions", StringComparison.Ordinal)
            || name.EndsWith("Validator", StringComparison.Ordinal)
            || name.EndsWith("Middleware", StringComparison.Ordinal))
            return true;

        // Blazor component/page partial classes: the .razor markup is the real declaration site.
        return Directory.EnumerateFiles(
            Path.Combine(SourceFiles.RepoRoot, "Applications", "Web"),
            $"{name}.razor",
            SearchOption.AllDirectories).Any();
    }
}
