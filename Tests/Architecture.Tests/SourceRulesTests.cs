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
