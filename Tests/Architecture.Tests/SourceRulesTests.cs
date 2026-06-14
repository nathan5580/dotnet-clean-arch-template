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
}
